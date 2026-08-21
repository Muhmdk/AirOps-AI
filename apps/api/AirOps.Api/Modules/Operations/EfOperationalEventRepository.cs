using AirOps.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirOps.Api.Modules.Operations;

public sealed class EfOperationalEventRepository(AirOpsDbContext database)
    : IOperationalEventRepository
{
    public async Task<IReadOnlyList<OperationalEvent>> SearchAsync(
        OperationalEventSeverity? severity,
        OperationalEventCategory? category,
        int limit,
        CancellationToken cancellationToken)
    {
        IQueryable<OperationalEvent> query = database.OperationalEvents.AsNoTracking();
        if (severity is not null)
            query = query.Where(item => item.Severity == severity);
        if (category is not null)
            query = query.Where(item => item.Category == category);

        return await query.OrderByDescending(item => item.OccurredAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
