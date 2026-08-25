namespace AirOps.Api.Contracts;

public sealed record CreateDisruptionRequest(
    string Type,
    string Severity,
    string Airport,
    string FlightId,
    int DurationMinutes);

public sealed record DisruptionResponse(
    string Id,
    string Type,
    string Severity,
    string Status,
    string Airport,
    string PrimaryFlight,
    DateTimeOffset StartedAt,
    int DurationMinutes,
    string Description,
    NetworkImpactResponse Impact,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);

public sealed record NetworkImpactResponse(
    int AffectedFlights,
    int AffectedPassengers,
    int MissedConnections,
    int CrewAffected,
    int GateConflicts,
    int HotelRooms,
    int MealVouchers,
    int EstimatedCompensation,
    int EstimatedOperationalCost,
    int RecoveryMinutes,
    IReadOnlyList<ImpactedFlightResponse> Flights,
    IReadOnlyList<PassengerConnectionImpactResponse> Connections,
    IReadOnlyList<GateConflictImpactResponse> GateDetails,
    IReadOnlyList<CrewDutyImpactResponse> CrewDetails);

public sealed record ImpactedFlightResponse(
    string Id,
    string Route,
    int OriginalDelay,
    int PropagatedDelay,
    int Passengers,
    int MissedConnections,
    string Reason);

public sealed record PassengerConnectionImpactResponse(
    string InboundFlight,
    string OutboundFlight,
    string ConnectionAirport,
    int Passengers,
    int MinimumConnectionMinutes,
    int AvailableConnectionMinutes,
    string Status);

public sealed record GateConflictImpactResponse(
    string Airport,
    string Gate,
    string IncomingFlight,
    string OccupyingFlight,
    int OverlapMinutes,
    string Severity);

public sealed record CrewDutyImpactResponse(
    string CrewId,
    string FlightId,
    string Role,
    int ProjectedDutyMinutes,
    int LegalLimitMinutes,
    int RemainingMinutes,
    string Status);

public sealed record DisruptionAuditResponse(
    Guid Id,
    string DisruptionId,
    string Action,
    string Actor,
    DateTimeOffset Timestamp,
    string Summary,
    IReadOnlyList<NetworkMutationResponse> Changes);

public sealed record NetworkMutationResponse(
    string EntityType,
    string EntityId,
    string Field,
    string Before,
    string After);
