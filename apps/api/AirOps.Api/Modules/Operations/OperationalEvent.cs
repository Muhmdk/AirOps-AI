namespace AirOps.Api.Modules.Operations;

public enum OperationalEventType
{
    Risk,
    Delay,
    Gate,
    Ok,
}

public enum OperationalEventSeverity
{
    Critical,
    Warning,
    Information,
}

public enum OperationalEventCategory
{
    Weather,
    Flight,
    Gate,
    Aircraft,
    Passenger,
    Simulation,
}

public sealed class OperationalEvent
{
    private OperationalEvent() { }

    public OperationalEvent(
        Guid id,
        DateTimeOffset occurredAt,
        OperationalEventType type,
        string title,
        string detail,
        string accent,
        OperationalEventSeverity severity,
        string? entityType,
        string? entityId,
        OperationalEventCategory category,
        string? eventKey = null)
    {
        Id = id;
        OccurredAt = occurredAt;
        Type = type;
        Title = title;
        Detail = detail;
        Accent = accent;
        Severity = severity;
        EntityType = entityType;
        EntityId = entityId;
        Category = category;
        EventKey = eventKey;
    }

    public Guid Id { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public OperationalEventType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Detail { get; private set; } = string.Empty;
    public string Accent { get; private set; } = string.Empty;
    public OperationalEventSeverity Severity { get; private set; }
    public string? EntityType { get; private set; }
    public string? EntityId { get; private set; }
    public OperationalEventCategory Category { get; private set; }
    public string? EventKey { get; private set; }
}
