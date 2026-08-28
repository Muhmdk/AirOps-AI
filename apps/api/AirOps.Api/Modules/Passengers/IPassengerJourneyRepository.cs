namespace AirOps.Api.Modules.Passengers;

public interface IPassengerJourneyRepository
{
    Task<IReadOnlyList<PassengerJourney>> SearchAsync(
        string? search,
        PassengerJourneyStatus? status,
        string? flightId,
        CancellationToken cancellationToken);
    Task<PassengerJourney?> GetByIdAsync(
        string id,
        bool tracking,
        CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
