namespace AirOps.Api.Modules.Flights;

public enum FlightStatus
{
    OnTime,
    Delayed,
    Boarding,
    AtRisk,
    Cancelled,
}

public sealed record Flight(
    string Id,
    string OriginCode,
    string Origin,
    string DestinationCode,
    string Destination,
    DateTimeOffset ScheduledDeparture,
    DateTimeOffset ScheduledArrival,
    string AircraftRegistration,
    string AircraftType,
    string Gate,
    FlightStatus Status,
    int Risk,
    int DelayMinutes,
    int Passengers,
    int ConnectingPassengers,
    string RiskLabel)
{
    public DateTimeOffset EstimatedDeparture => ScheduledDeparture.AddMinutes(DelayMinutes);
    public DateTimeOffset EstimatedArrival => ScheduledArrival.AddMinutes(DelayMinutes);
    public string Route => $"{OriginCode} → {DestinationCode}";
}
