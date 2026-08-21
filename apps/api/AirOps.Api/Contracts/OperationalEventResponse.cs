namespace AirOps.Api.Contracts;

public sealed record OperationalEventResponse(
    Guid Id,
    DateTimeOffset Timestamp,
    string Time,
    string Type,
    string Title,
    string Detail,
    string Accent,
    string Severity,
    string? EntityType,
    string? EntityId,
    string Category);
