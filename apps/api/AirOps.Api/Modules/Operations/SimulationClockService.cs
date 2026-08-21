using AirOps.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirOps.Api.Modules.Operations;

public sealed class SimulationClockService(AirOpsDbContext database, TimeProvider timeProvider)
{
    public Task<SimulationClockState> GetAsync(CancellationToken cancellationToken) =>
        database.SimulationClocks.SingleAsync(
            item => item.Id == SimulationClockState.SingletonId, cancellationToken);

    public async Task<SimulationClockState> StartAsync(
        int minutesPerTick,
        CancellationToken cancellationToken)
    {
        var clock = await GetAsync(cancellationToken);
        clock.Start(minutesPerTick, timeProvider.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
        return clock;
    }

    public async Task<SimulationClockState> PauseAsync(CancellationToken cancellationToken)
    {
        var clock = await GetAsync(cancellationToken);
        clock.Pause(timeProvider.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
        return clock;
    }

    public async Task<SimulationClockState> AdvanceAsync(
        int minutes,
        CancellationToken cancellationToken)
    {
        var clock = await GetAsync(cancellationToken);
        var previousTime = clock.CurrentTime;
        clock.Advance(minutes, timeProvider.GetUtcNow());
        await GenerateFlightMilestonesAsync(previousTime, clock.CurrentTime, cancellationToken);
        await database.SaveChangesAsync(cancellationToken);
        return clock;
    }

    public async Task<SimulationClockState> ResetAsync(CancellationToken cancellationToken)
    {
        var generatedEvents = await database.OperationalEvents
            .Where(item => item.EventKey != null && item.EventKey.StartsWith("simulation:"))
            .ToListAsync(cancellationToken);
        database.OperationalEvents.RemoveRange(generatedEvents);
        var clock = await GetAsync(cancellationToken);
        clock.Reset(timeProvider.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
        return clock;
    }

    public async Task TickAsync(CancellationToken cancellationToken)
    {
        var clock = await GetAsync(cancellationToken);
        if (clock.Status == SimulationClockStatus.Running)
            await AdvanceAsync(clock.MinutesPerTick, cancellationToken);
    }

    private async Task GenerateFlightMilestonesAsync(
        DateTimeOffset previousTime,
        DateTimeOffset currentTime,
        CancellationToken cancellationToken)
    {
        var flights = await database.Flights.AsNoTracking()
            .Where(flight => flight.ScheduledDeparture > previousTime &&
                flight.ScheduledDeparture <= currentTime)
            .ToListAsync(cancellationToken);

        foreach (var flight in flights)
        {
            var eventKey = $"simulation:departure:{flight.Id}:{flight.ScheduledDeparture:O}";
            if (await database.OperationalEvents.AnyAsync(
                item => item.EventKey == eventKey, cancellationToken))
                continue;

            database.OperationalEvents.Add(new OperationalEvent(
                Guid.NewGuid(),
                flight.ScheduledDeparture,
                OperationalEventType.Ok,
                $"{flight.Id} departed",
                $"{flight.Route} · Gate {flight.Gate}",
                "green",
                OperationalEventSeverity.Information,
                "flight",
                flight.Id,
                OperationalEventCategory.Flight,
                eventKey));
        }
    }
}
