namespace AirOps.Api.Modules.Recovery;

public interface IRecoveryPlanRepository
{
    Task<IReadOnlyList<RecoveryPlan>> GetForDisruptionAsync(
        string disruptionId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<RecoveryPlan>> GetTrackedForDisruptionAsync(
        string disruptionId,
        CancellationToken cancellationToken);
    Task<RecoveryPlan?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecoveryAuditEntry>> GetAuditAsync(
        string? planId,
        CancellationToken cancellationToken);
    void AddRange(IEnumerable<RecoveryPlan> plans);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
