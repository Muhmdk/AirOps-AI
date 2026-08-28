using AirOps.Api.Modules.Passengers;

namespace AirOps.Api.Persistence;

public static class PassengerSeed
{
    private static readonly DateTimeOffset SeedTime =
        new(2026, 8, 6, 13, 12, 0, TimeSpan.Zero);

    public static IReadOnlyList<PassengerJourney> All =>
    [
        new("PAX-001", "7Q4K2M", "Aisha Rahman", 3, "Aeroplan 50K",
            "AC103", "AC205", "YYZ", "YVR", "YYC", 45, 16,
            PassengerJourneyStatus.AtRisk, 91,
            ["Wheelchair assistance", "Checked bags through to YYC"],
            ["AC125 · YYZ → YYC · 12:10", "AC211 · YVR → YYC · 14:25"],
            780, SeedTime),
        new("PAX-002", "M8T3LX", "Daniel Tremblay", 2, "Aeroplan 35K",
            "AC418", "AC834", "YYZ", "YUL", "YHZ", 45, 22,
            PassengerJourneyStatus.AtRisk, 84,
            ["French-language service", "Checked bags through to YHZ"],
            ["AC612 · YYZ → YHZ · 12:35", "AC836 · YUL → YHZ · 14:10"],
            520, SeedTime),
        new("PAX-003", "C2N9PW", "Sofia Chen", 1, "Aeroplan 75K",
            "AC791", "UA188", "YUL", "LAX", "SFO", 60, 0,
            PassengerJourneyStatus.Misconnected, 97,
            ["Priority protection", "Checked bag retrieval required"],
            ["UA522 · LAX → SFO · 15:20", "AC745 · YUL → SFO · 17:05"],
            940, SeedTime),
        new("PAX-004", "R5J1VD", "Marcus Johnson", 4, "Aeroplan Member",
            "AC156", "AC882", "YYC", "YYZ", "CDG", 60, 72,
            PassengerJourneyStatus.Protected, 28,
            ["Family seating", "Checked bags through to CDG"],
            ["AC872 · YYZ → CDG · 22:10"],
            0, SeedTime),
        new("PAX-005", "F6B7QA", "Elena Rossi", 2, "Aeroplan 25K",
            "AC882", "AF144", "YYZ", "CDG", "FCO", 75, 41,
            PassengerJourneyStatus.AtRisk, 76,
            ["Vegetarian meals", "Checked bags through to FCO"],
            ["AF1604 · CDG → FCO · 09:40", "AZ319 · CDG → FCO · 11:15"],
            610, SeedTime),
        new("PAX-006", "K4H8ZS", "Noah Williams", 1, "Aeroplan Super Elite",
            "AC103", "AC205", "YYZ", "YVR", "YYC", 45, 18,
            PassengerJourneyStatus.Rebooked, 18,
            ["Priority protection"],
            ["AC125 · YYZ → YYC · 12:10"],
            240, SeedTime,
            "AC125 · YYZ → YYC · 12:10",
            "Protected on the direct service before the connection window closed."),
    ];
}
