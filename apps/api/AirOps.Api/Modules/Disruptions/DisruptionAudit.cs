namespace AirOps.Api.Modules.Disruptions;

public enum DisruptionAuditAction
{
    Created,
    Resolved,
}

public sealed class DisruptionAuditEntry
{
    private readonly List<NetworkMutation> changes = [];

    private DisruptionAuditEntry() { }

    public DisruptionAuditEntry(
        Guid id,
        string disruptionId,
        DisruptionAuditAction action,
        string actor,
        DateTimeOffset timestamp,
        string summary,
        IEnumerable<NetworkMutation> mutations)
    {
        Id = id;
        DisruptionId = disruptionId;
        Action = action;
        Actor = actor;
        Timestamp = timestamp;
        Summary = summary;
        changes.AddRange(mutations);
    }

    public Guid Id { get; private set; }
    public string DisruptionId { get; private set; } = string.Empty;
    public DisruptionAuditAction Action { get; private set; }
    public string Actor { get; private set; } = string.Empty;
    public DateTimeOffset Timestamp { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public IReadOnlyCollection<NetworkMutation> Changes => changes;
}

public sealed class NetworkMutation
{
    private NetworkMutation() { }

    public NetworkMutation(
        Guid auditEntryId,
        string entityType,
        string entityId,
        string field,
        string beforeValue,
        string afterValue)
    {
        Id = Guid.NewGuid();
        AuditEntryId = auditEntryId;
        EntityType = entityType;
        EntityId = entityId;
        Field = field;
        BeforeValue = beforeValue;
        AfterValue = afterValue;
    }

    public Guid Id { get; private set; }
    public Guid AuditEntryId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string Field { get; private set; } = string.Empty;
    public string BeforeValue { get; private set; } = string.Empty;
    public string AfterValue { get; private set; } = string.Empty;
}
