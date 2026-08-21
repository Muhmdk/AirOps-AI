namespace AirOps.Api.Contracts;

public sealed record AircraftResponse(
    string Registration,
    string Type,
    string Family,
    string Status,
    string Location,
    string NextFlight,
    string NextDeparture,
    int Utilization,
    int Cycles,
    decimal Hours,
    int MaintenanceDue,
    int Health,
    int Seats,
    string Range);
