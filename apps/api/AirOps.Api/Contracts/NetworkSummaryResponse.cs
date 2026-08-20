namespace AirOps.Api.Contracts;

public sealed record NetworkSummaryResponse(
    int FlightsToday,
    int OnTime,
    int Delayed,
    int Boarding,
    int AtRisk,
    int Cancelled,
    int HighRisk,
    int Passengers,
    int ConnectingPassengers,
    int NetworkHealth);
