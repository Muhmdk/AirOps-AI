using AirOps.Api.Modules.Disruptions;

namespace AirOps.Api.Persistence;

public static class DisruptionSeed
{
    private static readonly DateTimeOffset OperationDate =
        new DateTimeOffset(2026, 8, 6, 0, 0, 0, TimeSpan.FromHours(-4)).ToUniversalTime();

    public static IReadOnlyList<Disruption> All { get; } =
    [
        Create("DSP-001", DisruptionType.SevereWeather, DisruptionSeverity.Critical,
            "YYZ", "AC103", 9, 8, 120),
        Create("DSP-002", DisruptionType.LateIncomingAircraft, DisruptionSeverity.High,
            "YYZ", "AC418", 9, 4, 75),
    ];

    private static Disruption Create(
        string id,
        DisruptionType type,
        DisruptionSeverity severity,
        string airport,
        string flightId,
        int hour,
        int minute,
        int durationMinutes)
    {
        var flight = FlightSeed.All.Single(item => item.Id == flightId);
        var startedAt = OperationDate.AddHours(hour).AddMinutes(minute);
        return new Disruption(
            id,
            type,
            severity,
            airport,
            flightId,
            startedAt,
            durationMinutes,
            $"{type.ToDisplayName()} affecting {airport} operations and the {flightId} aircraft rotation.",
            startedAt,
            DisruptionImpactCalculator.Calculate(id, type, severity, flight, FlightSeed.All));
    }
}
