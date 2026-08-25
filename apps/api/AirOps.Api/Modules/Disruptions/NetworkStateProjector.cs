using System.Globalization;
using AirOps.Api.Modules.Aircraft;
using AirOps.Api.Modules.Airports;
using AirOps.Api.Modules.Flights;
using AirOps.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using AircraftEntity = AirOps.Api.Modules.Aircraft.Aircraft;

namespace AirOps.Api.Modules.Disruptions;

public static class NetworkStateProjector
{
    private static readonly IReadOnlyDictionary<string, FlightBaseline> FlightBaselines =
        FlightSeed.All.ToDictionary(
            item => item.Id,
            item => new FlightBaseline(item.Status, item.Risk, item.DelayMinutes, item.RiskLabel),
            StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlyDictionary<string, AirportBaseline> AirportBaselines =
        AirportSeed.All.ToDictionary(
            item => item.Code,
            item => new AirportBaseline(
                item.Risk, item.Health, item.AverageDelay, item.AtRisk,
                item.GatesUsed, item.Weather),
            StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlyDictionary<string, AircraftBaseline> AircraftBaselines =
        AircraftSeed.All.ToDictionary(
            item => item.Registration,
            item => new AircraftBaseline(
                item.Status, item.Health, item.Utilization, item.MaintenanceDue),
            StringComparer.OrdinalIgnoreCase);

    public static async Task<TrackedNetworkState> LoadAsync(
        AirOpsDbContext database,
        CancellationToken cancellationToken) => new(
            await database.Flights.ToListAsync(cancellationToken),
            await database.Airports.ToListAsync(cancellationToken),
            await database.Aircraft.ToListAsync(cancellationToken));

    public static void Project(
        TrackedNetworkState network,
        IEnumerable<Disruption> activeDisruptions)
    {
        foreach (var flight in network.Flights)
        {
            var baseline = FlightBaselines[flight.Id];
            flight.RestoreOperationalState(
                baseline.Status, baseline.Risk, baseline.DelayMinutes, baseline.RiskLabel);
        }
        foreach (var airport in network.Airports)
        {
            var baseline = AirportBaselines[airport.Code];
            airport.RestoreOperationalState(
                baseline.Risk, baseline.Health, baseline.AverageDelay,
                baseline.AtRisk, baseline.GatesUsed, baseline.Weather);
        }
        foreach (var aircraft in network.Aircraft)
        {
            var baseline = AircraftBaselines[aircraft.Registration];
            aircraft.RestoreOperationalState(
                baseline.Status, baseline.Health, baseline.Utilization, baseline.MaintenanceDue);
        }

        foreach (var disruption in activeDisruptions
            .OrderBy(item => item.StartedAt)
            .ThenBy(item => item.CreatedAt)
            .ThenBy(item => item.Id))
            Apply(network, disruption);
    }

    public static NetworkStateSnapshot Capture(TrackedNetworkState network)
    {
        var values = new Dictionary<string, NetworkStateValue>(StringComparer.Ordinal);
        foreach (var flight in network.Flights)
        {
            Add(values, "Flight", flight.Id, "Status", flight.Status.ToString());
            Add(values, "Flight", flight.Id, "Delay", flight.DelayMinutes);
            Add(values, "Flight", flight.Id, "Risk", flight.Risk);
            Add(values, "Flight", flight.Id, "Risk label", flight.RiskLabel);
        }
        foreach (var airport in network.Airports)
        {
            Add(values, "Airport", airport.Code, "Risk", airport.Risk.ToString());
            Add(values, "Airport", airport.Code, "Health", airport.Health);
            Add(values, "Airport", airport.Code, "Average delay", airport.AverageDelay);
            Add(values, "Airport", airport.Code, "At-risk flights", airport.AtRisk);
            Add(values, "Airport", airport.Code, "Gates used", airport.GatesUsed);
            Add(values, "Airport", airport.Code, "Weather", airport.Weather);
        }
        foreach (var aircraft in network.Aircraft)
        {
            Add(values, "Aircraft", aircraft.Registration, "Status", aircraft.Status.ToString());
            Add(values, "Aircraft", aircraft.Registration, "Health", aircraft.Health);
            Add(values, "Aircraft", aircraft.Registration, "Utilization", aircraft.Utilization);
            Add(values, "Aircraft", aircraft.Registration, "Maintenance due", aircraft.MaintenanceDue);
        }
        return new NetworkStateSnapshot(values);
    }

    public static IReadOnlyList<NetworkMutation> Compare(
        Guid auditEntryId,
        NetworkStateSnapshot before,
        NetworkStateSnapshot after) =>
        after.Values
            .Where(item => before.Values.TryGetValue(item.Key, out var previous) &&
                previous.Value != item.Value.Value)
            .Select(item => new NetworkMutation(
                auditEntryId,
                item.Value.EntityType,
                item.Value.EntityId,
                item.Value.Field,
                before.Values[item.Key].Value,
                item.Value.Value))
            .ToList();

    private static void Apply(TrackedNetworkState network, Disruption disruption)
    {
        var flightBaselines = FlightBaselines;
        foreach (var impact in disruption.Flights)
        {
            var flight = network.Flights.FirstOrDefault(item =>
                item.Id.Equals(impact.FlightId, StringComparison.OrdinalIgnoreCase));
            if (flight is null)
                continue;
            flight.ApplyDisruption(
                impact.PropagatedDelay,
                disruption.Type.ToDisplayName(),
                flightBaselines[flight.Id].RiskLabel);
        }

        var airport = network.Airports.First(item =>
            item.Code.Equals(disruption.AirportCode, StringComparison.OrdinalIgnoreCase));
        airport.ApplyDisruption(
            disruption.Severity == DisruptionSeverity.Moderate,
            disruption.Type == DisruptionType.SevereWeather,
            disruption.DurationMinutes,
            disruption.RecoveryMinutes,
            disruption.AffectedFlights,
            disruption.GateConflicts);

        var aircraft = network.Aircraft.FirstOrDefault(item =>
            item.NextFlight.Equals(disruption.PrimaryFlightId, StringComparison.OrdinalIgnoreCase));
        aircraft?.ApplyDisruption(
            disruption.Type == DisruptionType.AircraftMaintenance,
            disruption.Severity == DisruptionSeverity.Critical,
            disruption.DurationMinutes);
    }

    private static void Add(
        IDictionary<string, NetworkStateValue> values,
        string entityType,
        string entityId,
        string field,
        object value)
    {
        var text = value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value.ToString() ?? string.Empty;
        values[$"{entityType}|{entityId}|{field}"] =
            new NetworkStateValue(entityType, entityId, field, text);
    }

    private sealed record FlightBaseline(
        FlightStatus Status, int Risk, int DelayMinutes, string RiskLabel);
    private sealed record AirportBaseline(
        AirportRisk Risk, int Health, int AverageDelay, int AtRisk, int GatesUsed, string Weather);
    private sealed record AircraftBaseline(
        AircraftStatus Status, int Health, int Utilization, int MaintenanceDue);
}

public sealed record TrackedNetworkState(
    IReadOnlyList<Flight> Flights,
    IReadOnlyList<Airport> Airports,
    IReadOnlyList<AircraftEntity> Aircraft);

public sealed record NetworkStateSnapshot(
    IReadOnlyDictionary<string, NetworkStateValue> Values);

public sealed record NetworkStateValue(
    string EntityType,
    string EntityId,
    string Field,
    string Value);
