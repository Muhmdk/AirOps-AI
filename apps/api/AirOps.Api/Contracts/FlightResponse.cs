namespace AirOps.Api.Contracts;

public sealed record FlightResponse(
    string Id,
    string Route,
    string OriginCode,
    string Origin,
    string DestinationCode,
    string Destination,
    DateTimeOffset ScheduledDeparture,
    DateTimeOffset EstimatedDeparture,
    DateTimeOffset ScheduledArrival,
    DateTimeOffset EstimatedArrival,
    string AircraftRegistration,
    string AircraftType,
    string Gate,
    string Status,
    int Risk,
    int DelayMinutes,
    int Passengers,
    int ConnectingPassengers,
    string RiskLabel);
