namespace AirOps.Api.Contracts;

public sealed record RecoveryPlanResponse(
    string Id,
    string DisruptionId,
    string Name,
    string Action,
    string Description,
    IReadOnlyList<string> FlightsAffected,
    IReadOnlyList<string> AircraftAffected,
    int PassengersAffected,
    int MissedConnections,
    int ExpectedDelayMinutes,
    int RecoveryMinutes,
    int EstimatedCost,
    string OperationalRisk,
    IReadOnlyList<string> Advantages,
    IReadOnlyList<string> Disadvantages,
    int Score,
    bool Recommended,
    RecoveryScoreBreakdownResponse ScoreBreakdown,
    string Status,
    bool RequiresSupervisor,
    DateTimeOffset CreatedAt);

public sealed record RecoveryScoreBreakdownResponse(
    int Delay,
    int Cost,
    int Passengers,
    int Risk);

public sealed record RecoveryDecisionRequest(
    string Notes,
    bool SupervisorOverride = false);

public sealed record RecoveryDecisionResponse(
    RecoveryPlanResponse Plan,
    RecoveryAuditResponse Audit);

public sealed record RecoveryAuditResponse(
    Guid Id,
    string PlanId,
    string DisruptionId,
    string Action,
    string Actor,
    string ActorRole,
    DateTimeOffset Timestamp,
    string Notes,
    bool SupervisorOverride,
    RecoveryOutcomeResponse Outcome);

public sealed record RecoveryOutcomeResponse(
    int DelayBefore,
    int DelayAfter,
    int CostBefore,
    int CostAfter,
    int MissedBefore,
    int MissedAfter);
