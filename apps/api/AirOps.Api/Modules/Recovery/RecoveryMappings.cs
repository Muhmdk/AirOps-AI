using AirOps.Api.Contracts;

namespace AirOps.Api.Modules.Recovery;

internal static class RecoveryMappings
{
    internal static RecoveryPlanResponse ToResponse(this RecoveryPlan plan) => new(
        plan.Id,
        plan.DisruptionId,
        plan.Name,
        plan.Action.ToDisplayName(),
        plan.Description,
        plan.FlightsAffected,
        plan.AircraftAffected,
        plan.PassengersAffected,
        plan.MissedConnections,
        plan.ExpectedDelayMinutes,
        plan.RecoveryMinutes,
        plan.EstimatedCost,
        plan.OperationalRisk.ToString(),
        plan.Advantages,
        plan.Disadvantages,
        plan.Score,
        plan.Recommended,
        new RecoveryScoreBreakdownResponse(
            plan.DelayScore,
            plan.CostScore,
            plan.PassengerScore,
            plan.RiskScore),
        plan.Status.ToString(),
        plan.RequiresSupervisor,
        plan.CreatedAt);

    internal static RecoveryAuditResponse ToResponse(this RecoveryAuditEntry entry) => new(
        entry.Id,
        entry.PlanId,
        entry.DisruptionId,
        entry.Action.ToString(),
        entry.Actor,
        entry.ActorRole,
        entry.Timestamp,
        entry.Notes,
        entry.SupervisorOverride,
        new RecoveryOutcomeResponse(
            entry.DelayBefore,
            entry.DelayAfter,
            entry.CostBefore,
            entry.CostAfter,
            entry.MissedBefore,
            entry.MissedAfter));
}
