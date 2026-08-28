using AirOps.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirOps.Api.Modules.Aircraft;

public sealed class EfAircraftRepository(AirOpsDbContext database) : IAircraftRepository
{
    public async Task<IReadOnlyList<Aircraft>> SearchAsync(
        string? search,
        AircraftStatus? status,
        AircraftFamily? family,
        CancellationToken cancellationToken)
    {
        IQueryable<Aircraft> query = database.Aircraft.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(aircraft =>
                aircraft.Registration.ToUpper().Contains(term) ||
                aircraft.Type.ToUpper().Contains(term) ||
                aircraft.Location.ToUpper().Contains(term) ||
                aircraft.NextFlight.ToUpper().Contains(term));
        }
        if (status is not null)
            query = query.Where(aircraft => aircraft.Status == status);
        if (family is not null)
            query = query.Where(aircraft => aircraft.Family == family);

        return await query.OrderBy(aircraft => aircraft.Registration).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Aircraft>> GetAllAsync(CancellationToken cancellationToken) =>
        await database.Aircraft.AsNoTracking().ToListAsync(cancellationToken);

    public Task<Aircraft?> GetByRegistrationAsync(
        string registration,
        CancellationToken cancellationToken) =>
        database.Aircraft.AsNoTracking().FirstOrDefaultAsync(
            aircraft => aircraft.Registration.ToUpper() == registration.ToUpper(),
            cancellationToken);
}
