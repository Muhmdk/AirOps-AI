using AirOps.Api.Modules.Aircraft;

namespace AirOps.Api.Persistence;

public static class AircraftSeed
{
    public static IReadOnlyList<Aircraft> All { get; } =
    [
        new("C-FVLX", "Boeing 787-9", AircraftFamily.Widebody, AircraftStatus.InService,
            "YYZ", "AC103", new TimeOnly(9, 15), 87, 2, 13.4m, 146, 94, 298, 14140),
        new("C-GROV", "Airbus A220-300", AircraftFamily.Narrowbody, AircraftStatus.Turnaround,
            "YYZ", "AC418", new TimeOnly(9, 40), 76, 4, 8.1m, 32, 78, 137, 6300),
        new("C-GFAF", "Airbus A330-300", AircraftFamily.Widebody, AircraftStatus.InService,
            "YUL", "AC791", new TimeOnly(10, 5), 91, 2, 14.8m, 84, 88, 297, 11750),
        new("C-FSIP", "Boeing 737 MAX 8", AircraftFamily.Narrowbody, AircraftStatus.Turnaround,
            "YYC", "AC156", new TimeOnly(10, 20), 82, 5, 9.7m, 61, 86, 169, 6570),
        new("C-FITL", "Boeing 777-300ER", AircraftFamily.Widebody, AircraftStatus.Available,
            "YYZ", "AC882", new TimeOnly(20, 45), 48, 1, 7.3m, 212, 97, 400, 13650),
        new("C-GJYE", "Airbus A320-200", AircraftFamily.Narrowbody, AircraftStatus.Unavailable,
            "YUL", "Unassigned", null, 0, 0, 0m, 0, 42, 146, 6150),
    ];
}
