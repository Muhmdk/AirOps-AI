namespace AirOps.Api.Contracts;

public sealed record AirportResponse(
    string Code,
    string Name,
    string City,
    string Province,
    string Timezone,
    string Risk,
    int Health,
    int AverageDelay,
    int Departures,
    int Arrivals,
    int AtRisk,
    int GatesUsed,
    int GatesTotal,
    string Weather,
    int Temperature,
    string Wind,
    string Visibility);
