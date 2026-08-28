using AirOps.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirOps.Api.Modules.Recovery;

public sealed class EfRecoveryPlanRepository(AirOpsDbContext database)
    : IRecoveryPlanRepository
{
    public async Task<IReadOnlyList<RecoveryPlan>> GetForDisruptionAsync(
        string disruptionId,
        CancellationToken cancellationToken) =>
        await database.RecoveryPlans.AsNoTracking()
            .Where(item => item.DisruptionId == disruptionId.ToUpperInvariant())
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RecoveryPlan>> GetTrackedForDisruptionAsync(
        string disruptionId,
        CancellationToken cancellationToken) =>
        await database.RecoveryPlans
            .Where(item => item.DisruptionId == disruptionId.ToUpperInvariant())
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

    public Task<RecoveryPlan?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        database.RecoveryPlans.SingleOrDefaultAsync(
            item => item.Id == id.ToUpperInvariant(), cancellationToken);

    public async Task<IReadOnlyList<RecoveryAuditEntry>> GetAuditAsync(
        string? planId,
        CancellationToken cancellationToken)
    {
        IQueryable<RecoveryAuditEntry> query = database.RecoveryAuditEntries.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(planId))
        {
            var normalized = planId.Trim().ToUpperInvariant();
            query = query.Where(item => item.PlanId == normalized);
        }
        return await query.OrderByDescending(item => item.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public void AddRange(IEnumerable<RecoveryPlan> plans) =>
        database.RecoveryPlans.AddRange(plans);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        database.SaveChangesAsync(cancellationToken);
}
