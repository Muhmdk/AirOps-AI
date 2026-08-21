using AirOps.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirOps.Api.Modules.Airports;

public sealed class EfAirportRepository(AirOpsDbContext database) : IAirportRepository
{
    public async Task<IReadOnlyList<Airport>> SearchAsync(
        string? search,
        AirportRisk? risk,
        CancellationToken cancellationToken)
    {
        IQueryable<Airport> query = database.Airports.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(airport =>
                airport.Code.ToUpper().Contains(term) ||
                airport.Name.ToUpper().Contains(term) ||
                airport.City.ToUpper().Contains(term));
        }
        if (risk is not null)
            query = query.Where(airport => airport.Risk == risk);

        return await query.OrderBy(airport => airport.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Airport>> GetAllAsync(CancellationToken cancellationToken) =>
        await database.Airports.AsNoTracking().ToListAsync(cancellationToken);

    public Task<Airport?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        database.Airports.AsNoTracking().FirstOrDefaultAsync(
            airport => airport.Code.ToUpper() == code.ToUpper(), cancellationToken);
}
