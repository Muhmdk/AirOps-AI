using AirOps.Api.Modules.Operations;
using AirOps.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirOps.Api.Modules.Passengers;

public enum PassengerRebookError
{
    None,
    NotFound,
    AlreadyRebooked,
    InvalidAlternative,
}

public sealed record PassengerRebookResult(
    PassengerJourney? Journey,
    PassengerRebookError Error = PassengerRebookError.None);

public sealed class PassengerService(
    AirOpsDbContext database,
    IPassengerJourneyRepository repository,
    TimeProvider timeProvider)
{
    public async Task<PassengerRebookResult> RebookAsync(
        string id,
        string alternativeFlight,
        string notes,
        CancellationToken cancellationToken)
    {
        var journey = await repository.GetByIdAsync(id, true, cancellationToken);
        if (journey is null)
            return new(null, PassengerRebookError.NotFound);
        if (journey.Status == PassengerJourneyStatus.Rebooked)
            return new(journey, PassengerRebookError.AlreadyRebooked);
        var alternative = journey.AlternativeFlights.FirstOrDefault(item =>
            string.Equals(item, alternativeFlight.Trim(), StringComparison.OrdinalIgnoreCase));
        if (alternative is null)
            return new(journey, PassengerRebookError.InvalidAlternative);

        var now = timeProvider.GetUtcNow();
        journey.Rebook(alternative, notes.Trim(), now);
        var operationalTime = await database.SimulationClocks
            .Where(item => item.Id == SimulationClockState.SingletonId)
            .Select(item => item.CurrentTime)
            .SingleAsync(cancellationToken);
        database.OperationalEvents.Add(new OperationalEvent(
            Guid.NewGuid(),
            operationalTime,
            OperationalEventType.Ok,
            $"Passenger journey rebooked · {journey.BookingReference}",
            $"{journey.PartySize} traveler{(journey.PartySize == 1 ? "" : "s")} protected on {alternative.Split('·')[0].Trim()}",
            "green",
            OperationalEventSeverity.Information,
            "passenger",
            journey.Id,
            OperationalEventCategory.Passenger,
            $"passenger:{journey.Id}:rebooked"));
        await repository.SaveChangesAsync(cancellationToken);
        return new(journey);
    }
}
