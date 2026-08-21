namespace AirOps.Api.Contracts;

public sealed record SimulationClockResponse(
    DateTimeOffset CurrentTime,
    string Status,
    int MinutesPerTick,
    DateTimeOffset UpdatedAt);

public sealed record StartSimulationRequest(int MinutesPerTick = 1);

public sealed record AdvanceSimulationRequest(int Minutes);
