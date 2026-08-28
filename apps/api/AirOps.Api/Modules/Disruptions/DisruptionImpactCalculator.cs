using AirOps.Api.Modules.Flights;

namespace AirOps.Api.Modules.Disruptions;

public static class DisruptionImpactCalculator
{
    private static readonly IReadOnlyDictionary<DisruptionType, int> TypeDelay =
        new Dictionary<DisruptionType, int>
        {
            [DisruptionType.SevereWeather] = 70,
            [DisruptionType.AircraftMaintenance] = 95,
            [DisruptionType.LateIncomingAircraft] = 45,
            [DisruptionType.GateConflict] = 25,
            [DisruptionType.AirportCongestion] = 35,
            [DisruptionType.CrewTimingIssue] = 60,
            [DisruptionType.RunwayClosure] = 110,
            [DisruptionType.AirTrafficRestriction] = 50,
        };

    private static readonly IReadOnlyDictionary<string, string[]> Rotations =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["AC103"] = ["AC103", "AC205", "AC221"],
            ["AC418"] = ["AC418", "AC522"],
            ["AC791"] = ["AC791", "AC834"],
        };

    public static CalculatedNetworkImpact Calculate(
        string disruptionId,
        DisruptionType type,
        DisruptionSeverity severity,
        Flight primaryFlight,
        IReadOnlyCollection<Flight> networkFlights)
    {
        var severityFactor = severity switch
        {
            DisruptionSeverity.Critical => 1.3,
            DisruptionSeverity.High => 1,
            _ => 0.7,
        };
        var primaryDelay = (int)Math.Round(
            TypeDelay[type] * severityFactor, MidpointRounding.AwayFromZero);
        var flightsById = networkFlights.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        var rotation = Rotations.TryGetValue(primaryFlight.Id, out var configured)
            ? configured
            : [primaryFlight.Id];

        var flights = rotation.Select((flightId, index) =>
        {
            flightsById.TryGetValue(flightId, out var flight);
            var passengers = flight?.Passengers ?? Math.Max(90, primaryFlight.Passengers - index * 45);
            var propagatedDelay = Math.Max(12, (int)Math.Round(
                primaryDelay * Math.Pow(0.72, index), MidpointRounding.AwayFromZero));
            return new ImpactedFlight(
                disruptionId,
                index,
                flightId,
                flight?.Route ?? (index == 1 ? "YVR → YYC" : "YYC → YVR"),
                index == 0 ? primaryFlight.DelayMinutes : 0,
                propagatedDelay,
                passengers,
                (int)Math.Round((flight?.ConnectingPassengers ?? 24) *
                    (propagatedDelay / 100d), MidpointRounding.AwayFromZero),
                index == 0 ? type.ToDisplayName() : $"Aircraft rotation from {primaryFlight.Id}");
        }).ToList();

        var connections = flights.Select((flight, index) =>
        {
            var available = Math.Max(-15, 55 - flight.PropagatedDelay);
            return new PassengerConnectionImpact(
                disruptionId,
                index,
                flight.FlightId,
                index == 0 ? "AC205" : $"AC{340 + index * 18}",
                RouteDestination(flight.Route),
                Math.Max(1, flight.MissedConnections),
                45,
                available,
                available < 0 ? "Missed" : available < 45 ? "At risk" : "Protected");
        }).ToList();

        var gates = flights.Skip(1)
            .Where(item => item.PropagatedDelay >= 25)
            .Select((flight, index) => new GateConflictImpact(
                disruptionId,
                index,
                RouteOrigin(flight.Route),
                $"C{42 + index}",
                flight.FlightId,
                $"AC{340 + index * 22}",
                Math.Max(8, flight.PropagatedDelay - 18),
                flight.PropagatedDelay > 50 ? "Critical" : "Warning"))
            .ToList();

        var crews = flights.Select((flight, index) =>
        {
            var projected = 650 + flight.PropagatedDelay + index * 35;
            var remaining = 780 - projected;
            return new CrewDutyImpact(
                disruptionId,
                index,
                $"CREW-{118 + index}",
                flight.FlightId,
                index == 0 ? "Flight deck" : "Cabin crew",
                projected,
                780,
                remaining,
                remaining < 0 ? "Exceeded" : remaining < 60 ? "At risk" : "Monitor");
        }).ToList();

        var multiplier = severity switch
        {
            DisruptionSeverity.Critical => 1.25,
            DisruptionSeverity.High => 1,
            _ => 0.75,
        };
        var affectedPassengers = flights.Sum(item => item.Passengers);
        var missedConnections = connections.Where(item => item.Status == "Missed")
            .Sum(item => item.Passengers);
        var delayMinutes = flights.Sum(item => item.PropagatedDelay);

        return new CalculatedNetworkImpact(
            flights.Count,
            affectedPassengers,
            missedConnections,
            crews.Sum(item => item.Status == "Monitor" ? 0 : 7),
            gates.Count,
            Round(missedConnections * 0.22 * multiplier),
            Round(affectedPassengers * 0.38 * multiplier),
            Round(missedConnections * 420 * multiplier),
            Round((delayMinutes * 310 + affectedPassengers * 48 + missedConnections * 260) * multiplier),
            flights.Max(item => item.PropagatedDelay) + 45,
            flights,
            connections,
            gates,
            crews);
    }

    private static int Round(double value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private static string RouteOrigin(string route) => RoutePart(route, 0);
    private static string RouteDestination(string route) => RoutePart(route, 1);

    private static string RoutePart(string route, int index)
    {
        var parts = route.Split('→', StringSplitOptions.TrimEntries);
        return parts.Length > index ? parts[index] : route;
    }
}
