namespace AirOps.Api.Modules.Disruptions;

public interface IDisruptionRepository
{
    Task<IReadOnlyList<Disruption>> SearchAsync(
        DisruptionStatus? status,
        DisruptionSeverity? severity,
        string? airport,
        CancellationToken cancellationToken);
    Task<Disruption?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<string> NextIdAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DisruptionAuditEntry>> GetAuditAsync(
        string disruptionId,
        CancellationToken cancellationToken);
    void Add(Disruption disruption);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
