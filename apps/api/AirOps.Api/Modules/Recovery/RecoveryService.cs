using AirOps.Api.Modules.Aircraft;
using AirOps.Api.Modules.Disruptions;
using AirOps.Api.Modules.Operations;
using AirOps.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirOps.Api.Modules.Recovery;

public enum RecoveryOperationError
{
    None,
    DisruptionNotFound,
    DisruptionNotActive,
    PlanNotFound,
    PlanNotProposed,
    SupervisorRequired,
}

public sealed record RecoveryPlanResult(
    IReadOnlyList<RecoveryPlan> Plans,
    RecoveryOperationError Error = RecoveryOperationError.None,
    bool Created = false);

public sealed record RecoveryDecisionResult(
    RecoveryPlan? Plan,
    RecoveryAuditEntry? Audit,
    RecoveryOperationError Error = RecoveryOperationError.None);

public sealed class RecoveryService(
    AirOpsDbContext database,
    IRecoveryPlanRepository repository,
    IDisruptionRepository disruptions,
    DisruptionService disruptionService,
    TimeProvider timeProvider)
{
    public async Task<RecoveryPlanResult> GenerateAsync(
        string disruptionId,
        CancellationToken cancellationToken)
    {
        var existing = await repository.GetForDisruptionAsync(
            disruptionId, cancellationToken);
        if (existing.Count > 0)
            return new(existing);

        var disruption = await disruptions.GetByIdAsync(disruptionId, cancellationToken);
        if (disruption is null)
            return new([], RecoveryOperationError.DisruptionNotFound);
        if (disruption.Status == DisruptionStatus.Resolved)
            return new([], RecoveryOperationError.DisruptionNotActive);

        var availableAircraft = await database.Aircraft.AsNoTracking()
            .Where(item => item.Status == AircraftStatus.Available)
            .OrderBy(item => item.Registration)
            .Select(item => item.Registration)
            .FirstOrDefaultAsync(cancellationToken);
        var plans = RecoveryPlanGenerator.Generate(
            disruption, availableAircraft, timeProvider.GetUtcNow());
        repository.AddRange(plans);
        await repository.SaveChangesAsync(cancellationToken);
        return new(plans, Created: true);
    }

    public async Task<RecoveryDecisionResult> RejectAsync(
        string planId,
        string notes,
        CancellationToken cancellationToken)
    {
        var plan = await repository.GetByIdAsync(planId, cancellationToken);
        if (plan is null)
            return new(null, null, RecoveryOperationError.PlanNotFound);
        if (!plan.Reject())
            return new(plan, null, RecoveryOperationError.PlanNotProposed);

        var siblings = await repository.GetTrackedForDisruptionAsync(
            plan.DisruptionId, cancellationToken);
        var next = siblings.FirstOrDefault(item => item.Status == RecoveryPlanStatus.Proposed);
        foreach (var sibling in siblings)
            sibling.SetRecommended(sibling.Id == next?.Id);
        var disruption = await disruptions.GetByIdAsync(plan.DisruptionId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var audit = CreateAudit(
            plan,
            disruption,
            RecoveryDecisionAction.Rejected,
            "Maya Chen",
            "Operations Controller",
            notes,
            false,
            unchanged: true,
            now);
        database.RecoveryAuditEntries.Add(audit);
        database.OperationalEvents.Add(await CreateEventAsync(
            plan,
            disruption,
            OperationalEventType.Gate,
            $"Recovery rejected · {plan.Name}",
            $"{plan.DisruptionId} remains active for further recovery review",
            "blue",
            $"recovery:{plan.Id}:rejected",
            cancellationToken));
        await repository.SaveChangesAsync(cancellationToken);
        return new(plan, audit);
    }

    public async Task<RecoveryDecisionResult> ApproveAsync(
        string planId,
        string notes,
        bool supervisorOverride,
        CancellationToken cancellationToken)
    {
        var plan = await repository.GetByIdAsync(planId, cancellationToken);
        if (plan is null)
            return new(null, null, RecoveryOperationError.PlanNotFound);
        if (plan.Status != RecoveryPlanStatus.Proposed)
            return new(plan, null, RecoveryOperationError.PlanNotProposed);
        if (plan.RequiresSupervisor && !supervisorOverride)
            return new(plan, null, RecoveryOperationError.SupervisorRequired);

        var disruption = await disruptions.GetByIdAsync(plan.DisruptionId, cancellationToken);
        if (disruption is null)
            return new(plan, null, RecoveryOperationError.DisruptionNotFound);
        if (disruption.Status == DisruptionStatus.Resolved)
            return new(plan, null, RecoveryOperationError.DisruptionNotActive);

        var siblings = await repository.GetTrackedForDisruptionAsync(
            plan.DisruptionId, cancellationToken);
        plan.Approve();
        foreach (var sibling in siblings.Where(item => item.Id != plan.Id))
            sibling.Reject();

        await disruptionService.ResolveAsync(disruption.Id, cancellationToken);
        var network = await NetworkStateProjector.LoadAsync(database, cancellationToken);
        RecoveryStateProjector.Apply(network, plan, disruption);
        var now = timeProvider.GetUtcNow();
        var audit = CreateAudit(
            plan,
            disruption,
            RecoveryDecisionAction.Approved,
            supervisorOverride ? "Alex Morgan" : "Maya Chen",
            supervisorOverride ? "Operations Supervisor" : "Operations Controller",
            notes,
            supervisorOverride,
            unchanged: false,
            now);
        database.RecoveryAuditEntries.Add(audit);
        database.OperationalEvents.Add(await CreateEventAsync(
            plan,
            disruption,
            OperationalEventType.Ok,
            $"Recovery approved · {plan.Name}",
            $"{plan.DisruptionId} · {plan.ExpectedDelayMinutes} min expected delay",
            "green",
            $"recovery:{plan.Id}:approved",
            cancellationToken));
        await repository.SaveChangesAsync(cancellationToken);
        return new(plan, audit);
    }

    private static RecoveryAuditEntry CreateAudit(
        RecoveryPlan plan,
        Disruption? disruption,
        RecoveryDecisionAction action,
        string actor,
        string actorRole,
        string notes,
        bool supervisorOverride,
        bool unchanged,
        DateTimeOffset timestamp)
    {
        var delayBefore = disruption?.Flights.OrderBy(item => item.Sequence)
            .FirstOrDefault()?.PropagatedDelay ?? 0;
        var costBefore = disruption?.EstimatedOperationalCost ?? 0;
        var missedBefore = disruption?.MissedConnections ?? 0;
        return new RecoveryAuditEntry(
            Guid.NewGuid(),
            plan.Id,
            plan.DisruptionId,
            action,
            actor,
            actorRole,
            timestamp,
            notes,
            supervisorOverride,
            delayBefore,
            unchanged ? delayBefore : plan.ExpectedDelayMinutes,
            costBefore,
            unchanged ? costBefore : plan.EstimatedCost,
            missedBefore,
            unchanged ? missedBefore : plan.MissedConnections);
    }

    private async Task<OperationalEvent> CreateEventAsync(
        RecoveryPlan plan,
        Disruption? disruption,
        OperationalEventType type,
        string title,
        string detail,
        string accent,
        string eventKey,
        CancellationToken cancellationToken)
    {
        var operationalTime = await database.SimulationClocks
            .Where(item => item.Id == SimulationClockState.SingletonId)
            .Select(item => item.CurrentTime)
            .SingleAsync(cancellationToken);
        return new OperationalEvent(
            Guid.NewGuid(),
            operationalTime,
            type,
            title,
            detail,
            accent,
            OperationalEventSeverity.Information,
            "flight",
            disruption?.PrimaryFlightId,
            OperationalEventCategory.Flight,
            eventKey);
    }
}
