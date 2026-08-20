namespace AirOps.Api.Modules.Flights;

public sealed class SeededFlightRepository : IFlightRepository
{
    private static readonly DateTimeOffset OperationDate =
        new(2026, 8, 6, 0, 0, 0, TimeSpan.FromHours(-4));

    private readonly IReadOnlyList<Flight> flights =
    [
        Create("AC103", "YYZ", "Toronto", "YVR", "Vancouver", 9, 15, 11, 28,
            "C-FVLX", "Boeing 787-9", "D24", FlightStatus.AtRisk, 82, 68, 286, 47,
            "Severe weather"),
        Create("AC418", "YYZ", "Toronto", "YUL", "Montréal", 9, 40, 10, 58,
            "C-GROV", "Airbus A220-300", "D31", FlightStatus.Delayed, 71, 42, 124, 31,
            "Late inbound aircraft"),
        Create("AC791", "YUL", "Montréal", "LAX", "Los Angeles", 10, 5, 12, 49,
            "C-GFAF", "Airbus A330-300", "A52", FlightStatus.AtRisk, 67, 36, 241, 38,
            "Short turnaround"),
        Create("AC156", "YYC", "Calgary", "YYZ", "Toronto", 10, 20, 16, 4,
            "C-FSIP", "Boeing 737 MAX 8", "C18", FlightStatus.Boarding, 43, 12, 171, 22,
            "Airport congestion"),
        Create("AC882", "YYZ", "Toronto", "CDG", "Paris", 20, 45, 34, 5,
            "C-FITL", "Boeing 777-300ER", "E73", FlightStatus.OnTime, 18, 0, 356, 64,
            "Normal operations"),
    ];

    public IReadOnlyList<Flight> GetAll() => flights;

    public Flight? GetById(string id) => flights.FirstOrDefault(flight =>
        string.Equals(flight.Id, id, StringComparison.OrdinalIgnoreCase));

    private static Flight Create(
        string id,
        string originCode,
        string origin,
        string destinationCode,
        string destination,
        int departureHour,
        int departureMinute,
        int arrivalHour,
        int arrivalMinute,
        string aircraftRegistration,
        string aircraftType,
        string gate,
        FlightStatus status,
        int risk,
        int delayMinutes,
        int passengers,
        int connectingPassengers,
        string riskLabel)
    {
        var arrivalDayOffset = arrivalHour >= 24 ? 1 : 0;
        var arrival = OperationDate.AddDays(arrivalDayOffset)
            .AddHours(arrivalHour % 24)
            .AddMinutes(arrivalMinute);
        return new Flight(
            id,
            originCode,
            origin,
            destinationCode,
            destination,
            OperationDate.AddHours(departureHour).AddMinutes(departureMinute),
            arrival,
            aircraftRegistration,
            aircraftType,
            gate,
            status,
            risk,
            delayMinutes,
            passengers,
            connectingPassengers,
            riskLabel);
    }
}
