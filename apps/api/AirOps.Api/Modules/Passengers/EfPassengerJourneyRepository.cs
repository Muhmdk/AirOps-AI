using AirOps.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirOps.Api.Modules.Passengers;

public sealed class EfPassengerJourneyRepository(AirOpsDbContext database)
    : IPassengerJourneyRepository
{
    public async Task<IReadOnlyList<PassengerJourney>> SearchAsync(
        string? search,
        PassengerJourneyStatus? status,
        string? flightId,
        CancellationToken cancellationToken)
    {
        IQueryable<PassengerJourney> query = database.PassengerJourneys.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(item =>
                item.Id.ToUpper().Contains(term) ||
                item.BookingReference.ToUpper().Contains(term) ||
                item.LeadPassenger.ToUpper().Contains(term) ||
                item.CurrentFlightId.ToUpper().Contains(term) ||
                item.ConnectingFlightId.ToUpper().Contains(term) ||
                item.OriginCode.ToUpper().Contains(term) ||
                item.DestinationCode.ToUpper().Contains(term));
        }
        if (status is not null)
            query = query.Where(item => item.Status == status);
        if (!string.IsNullOrWhiteSpace(flightId))
        {
            var flight = flightId.Trim().ToUpperInvariant();
            query = query.Where(item =>
                item.CurrentFlightId.ToUpper() == flight ||
                item.ConnectingFlightId.ToUpper() == flight);
        }

        return await query
            .OrderByDescending(item => item.RiskScore)
            .ThenBy(item => item.BookingReference)
            .ToListAsync(cancellationToken);
    }

    public Task<PassengerJourney?> GetByIdAsync(
        string id,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var query = tracking
            ? database.PassengerJourneys.AsQueryable()
            : database.PassengerJourneys.AsNoTracking();
        return query.FirstOrDefaultAsync(
            item => item.Id.ToUpper() == id.ToUpper(), cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        database.SaveChangesAsync(cancellationToken);
}
