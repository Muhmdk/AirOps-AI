namespace AirOps.Api.Modules.Disruptions;

public static class DisruptionTypeNames
{
    public static string ToDisplayName(this DisruptionType type) => type switch
    {
        DisruptionType.SevereWeather => "Severe weather",
        DisruptionType.AircraftMaintenance => "Aircraft maintenance",
        DisruptionType.LateIncomingAircraft => "Late incoming aircraft",
        DisruptionType.GateConflict => "Gate conflict",
        DisruptionType.AirportCongestion => "Airport congestion",
        DisruptionType.CrewTimingIssue => "Crew timing issue",
        DisruptionType.RunwayClosure => "Runway closure",
        DisruptionType.AirTrafficRestriction => "Air traffic restriction",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static bool TryParse(string value, out DisruptionType type)
    {
        var normalized = value.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
        return Enum.TryParse(normalized, true, out type);
    }
}
