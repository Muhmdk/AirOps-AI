namespace AirOps.Api.Modules.Recovery;

public enum RecoveryDecisionAction
{
    Approved,
    Rejected,
}

public sealed class RecoveryAuditEntry
{
    private RecoveryAuditEntry() { }

    public RecoveryAuditEntry(
        Guid id,
        string planId,
        string disruptionId,
        RecoveryDecisionAction action,
        string actor,
        string actorRole,
        DateTimeOffset timestamp,
        string notes,
        bool supervisorOverride,
        int delayBefore,
        int delayAfter,
        int costBefore,
        int costAfter,
        int missedBefore,
        int missedAfter)
    {
        Id = id;
        PlanId = planId;
        DisruptionId = disruptionId;
        Action = action;
        Actor = actor;
        ActorRole = actorRole;
        Timestamp = timestamp;
        Notes = notes;
        SupervisorOverride = supervisorOverride;
        DelayBefore = delayBefore;
        DelayAfter = delayAfter;
        CostBefore = costBefore;
        CostAfter = costAfter;
        MissedBefore = missedBefore;
        MissedAfter = missedAfter;
    }

    public Guid Id { get; private set; }
    public string PlanId { get; private set; } = string.Empty;
    public string DisruptionId { get; private set; } = string.Empty;
    public RecoveryDecisionAction Action { get; private set; }
    public string Actor { get; private set; } = string.Empty;
    public string ActorRole { get; private set; } = string.Empty;
    public DateTimeOffset Timestamp { get; private set; }
    public string Notes { get; private set; } = string.Empty;
    public bool SupervisorOverride { get; private set; }
    public int DelayBefore { get; private set; }
    public int DelayAfter { get; private set; }
    public int CostBefore { get; private set; }
    public int CostAfter { get; private set; }
    public int MissedBefore { get; private set; }
    public int MissedAfter { get; private set; }
}
