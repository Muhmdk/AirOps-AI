using AirOps.Api.Modules.Airports;

namespace AirOps.Api.Persistence;

public static class AirportSeed
{
    public static IReadOnlyList<Airport> All =>
    [
        new("YYZ", "Toronto Pearson International", "Toronto", "ON", "EDT",
            AirportRisk.High, 62, 38, 184, 177, 12, 58, 65, "Thunderstorms", 24,
            "SW 28 km/h", "5 km"),
        new("YUL", "Montréal–Trudeau International", "Montréal", "QC", "EDT",
            AirportRisk.Moderate, 78, 21, 116, 109, 5, 42, 52, "Light rain", 21,
            "W 14 km/h", "12 km"),
        new("YYC", "Calgary International", "Calgary", "AB", "MDT",
            AirportRisk.Low, 91, 8, 92, 88, 1, 31, 42, "Clear", 18,
            "NW 9 km/h", "24 km"),
        new("YVR", "Vancouver International", "Vancouver", "BC", "PDT",
            AirportRisk.Low, 94, 6, 128, 121, 2, 39, 50, "Partly cloudy", 17,
            "W 11 km/h", "20 km"),
        new("YWG", "Winnipeg Richardson International", "Winnipeg", "MB", "CDT",
            AirportRisk.Moderate, 81, 17, 51, 48, 3, 15, 22, "Overcast", 20,
            "S 19 km/h", "14 km"),
        new("YHZ", "Halifax Stanfield International", "Halifax", "NS", "ADT",
            AirportRisk.Low, 89, 9, 47, 44, 1, 18, 28, "Clear", 19,
            "SE 8 km/h", "25 km"),
    ];
}
