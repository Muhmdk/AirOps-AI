using AirOps.Api.Modules.Operations;

namespace AirOps.Api.Persistence;

public static class OperationalEventSeed
{
    private static readonly DateTimeOffset OperationDate =
        new DateTimeOffset(2026, 8, 6, 0, 0, 0, TimeSpan.FromHours(-4)).ToUniversalTime();

    public static IReadOnlyList<OperationalEvent> All { get; } =
    [
        Create(1, 9, 8, OperationalEventType.Risk, "Weather risk raised",
            "Toronto Pearson · Severe thunderstorm cell", "amber",
            OperationalEventSeverity.Critical, "airport", "YYZ", OperationalEventCategory.Weather),
        Create(2, 9, 4, OperationalEventType.Delay, "AC418 delayed 42 min",
            "Late incoming aircraft · Gate D31", "red",
            OperationalEventSeverity.Warning, "flight", "AC418", OperationalEventCategory.Flight),
        Create(3, 8, 57, OperationalEventType.Gate, "Gate change · AC791",
            "A48 → A52 · Montréal Trudeau", "blue",
            OperationalEventSeverity.Information, "flight", "AC791", OperationalEventCategory.Gate),
        Create(4, 8, 51, OperationalEventType.Ok, "AC302 departed",
            "Toronto → Ottawa · 3 min early", "green",
            OperationalEventSeverity.Information, "flight", "AC302", OperationalEventCategory.Flight),
        Create(5, 8, 46, OperationalEventType.Risk, "Aircraft unavailable · C-GJYE",
            "Technical inspection required · Montréal", "red",
            OperationalEventSeverity.Critical, "aircraft", "C-GJYE", OperationalEventCategory.Aircraft),
        Create(6, 8, 39, OperationalEventType.Risk, "Passenger connections at risk",
            "AC103 · 47 protected connections", "amber",
            OperationalEventSeverity.Warning, "flight", "AC103", OperationalEventCategory.Passenger),
    ];

    private static OperationalEvent Create(
        int id,
        int hour,
        int minute,
        OperationalEventType type,
        string title,
        string detail,
        string accent,
        OperationalEventSeverity severity,
        string entityType,
        string entityId,
        OperationalEventCategory category) =>
        new(
            Guid.Parse($"00000000-0000-0000-0000-{id:000000000000}"),
            OperationDate.AddHours(hour).AddMinutes(minute),
            type,
            title,
            detail,
            accent,
            severity,
            entityType,
            entityId,
            category,
            $"seed:{id}");
}
