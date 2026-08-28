namespace AirOps.Api.Modules.Operations;

public interface IOperationalEventRepository
{
    Task<IReadOnlyList<OperationalEvent>> SearchAsync(
        OperationalEventSeverity? severity,
        OperationalEventCategory? category,
        int limit,
        CancellationToken cancellationToken);
}
