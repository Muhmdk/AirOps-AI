using AirOps.Api.Modules.Airports;
using AirOps.Api.Modules.Flights;
using AirOps.Api.Modules.Operations;
using AirOps.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirOps.Api.Modules.Disruptions;

public enum DisruptionCreationError
{
    None,
    FlightNotFound,
    AirportNotFound,
}

public sealed record DisruptionCreationResult(
    Disruption? Disruption,
    DisruptionCreationError Error = DisruptionCreationError.None);

public sealed class DisruptionService(
    AirOpsDbContext database,
    IDisruptionRepository repository,
    IFlightRepository flights,
    IAirportRepository airports,
    TimeProvider timeProvider)
{
    public async Task<DisruptionCreationResult> CreateAsync(
        DisruptionType type,
        DisruptionSeverity severity,
        string airportCode,
        string flightId,
        int durationMinutes,
        CancellationToken cancellationToken)
    {
        var flight = await flights.GetByIdAsync(flightId, cancellationToken);
        if (flight is null)
            return new(null, DisruptionCreationError.FlightNotFound);

        var airport = await airports.GetByCodeAsync(airportCode, cancellationToken);
        if (airport is null)
            return new(null, DisruptionCreationError.AirportNotFound);

        var id = await repository.NextIdAsync(cancellationToken);
        var networkFlights = await flights.GetAllAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var operationalTime = await database.SimulationClocks
            .Where(item => item.Id == SimulationClockState.SingletonId)
            .Select(item => item.CurrentTime)
            .SingleAsync(cancellationToken);
        var impact = DisruptionImpactCalculator.Calculate(
            id, type, severity, flight, networkFlights);
        var disruption = new Disruption(
            id,
            type,
            severity,
            airport.Code,
            flight.Id,
            operationalTime,
            durationMinutes,
            $"{type.ToDisplayName()} affecting {airport.Code} operations and the {flight.Id} aircraft rotation.",
            now,
            impact);

        repository.Add(disruption);
        database.OperationalEvents.Add(new OperationalEvent(
            Guid.NewGuid(),
            operationalTime,
            OperationalEventType.Risk,
            $"{type.ToDisplayName()} · {flight.Id}",
            $"{impact.AffectedFlights} flights and {impact.AffectedPassengers} passengers affected",
            "red",
            severity == DisruptionSeverity.Moderate
                ? OperationalEventSeverity.Warning
                : OperationalEventSeverity.Critical,
            "flight",
            flight.Id,
            type == DisruptionType.SevereWeather
                ? OperationalEventCategory.Weather
                : OperationalEventCategory.Flight,
            $"disruption:{id}:created"));
        await repository.SaveChangesAsync(cancellationToken);
        return new(disruption);
    }

    public async Task<Disruption?> ResolveAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var disruption = await repository.GetByIdAsync(id, cancellationToken);
        if (disruption is null)
            return null;

        var now = timeProvider.GetUtcNow();
        if (disruption.Resolve(now))
        {
            var operationalTime = await database.SimulationClocks
                .Where(item => item.Id == SimulationClockState.SingletonId)
                .Select(item => item.CurrentTime)
                .SingleAsync(cancellationToken);
            database.OperationalEvents.Add(new OperationalEvent(
                Guid.NewGuid(),
                operationalTime,
                OperationalEventType.Ok,
                $"Disruption resolved · {disruption.Id}",
                $"{disruption.PrimaryFlightId} returned to recovery monitoring",
                "green",
                OperationalEventSeverity.Information,
                "flight",
                disruption.PrimaryFlightId,
                OperationalEventCategory.Flight,
                $"disruption:{disruption.Id}:resolved"));
            await repository.SaveChangesAsync(cancellationToken);
        }

        return disruption;
    }
}
