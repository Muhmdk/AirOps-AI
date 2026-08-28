using AirOps.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirOps.Api.Modules.Flights;

public sealed class EfFlightRepository(AirOpsDbContext database) : IFlightRepository
{
    public async Task<IReadOnlyList<Flight>> SearchAsync(
        string? search,
        FlightStatus? status,
        int? minRisk,
        CancellationToken cancellationToken)
    {
        IQueryable<Flight> query = database.Flights.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(flight =>
                flight.Id.ToUpper().Contains(term) ||
                flight.OriginCode.ToUpper().Contains(term) ||
                flight.DestinationCode.ToUpper().Contains(term) ||
                flight.Origin.ToUpper().Contains(term) ||
                flight.Destination.ToUpper().Contains(term));
        }

        if (status is not null)
            query = query.Where(flight => flight.Status == status);
        if (minRisk is not null)
            query = query.Where(flight => flight.Risk >= minRisk);

        return await query.OrderByDescending(flight => flight.Risk).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Flight>> GetAllAsync(CancellationToken cancellationToken) =>
        await database.Flights.AsNoTracking().ToListAsync(cancellationToken);

    public Task<Flight?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        database.Flights.AsNoTracking().FirstOrDefaultAsync(
            flight => flight.Id.ToUpper() == id.ToUpper(), cancellationToken);
}
