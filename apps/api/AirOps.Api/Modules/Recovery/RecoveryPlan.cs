namespace AirOps.Api.Modules.Recovery;

public enum RecoveryActionType
{
    MaintainRotation,
    SwapAircraft,
    HoldConnectingFlight,
    ChangeGate,
    CancelDownstreamFlight,
    RebookPassengers,
}

public enum OperationalRisk
{
    Low,
    Medium,
    High,
}

public enum RecoveryPlanStatus
{
    Proposed,
    Approved,
    Rejected,
}

public static class RecoveryActionNames
{
    public static string ToDisplayName(this RecoveryActionType action) => action switch
    {
        RecoveryActionType.MaintainRotation => "Maintain rotation",
        RecoveryActionType.SwapAircraft => "Swap aircraft",
        RecoveryActionType.HoldConnectingFlight => "Hold connecting flight",
        RecoveryActionType.ChangeGate => "Change gate",
        RecoveryActionType.CancelDownstreamFlight => "Cancel downstream flight",
        RecoveryActionType.RebookPassengers => "Rebook passengers",
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };
}

public sealed class RecoveryPlan
{
    private RecoveryPlan() { }

    public RecoveryPlan(
        string id,
        string disruptionId,
        string name,
        RecoveryActionType action,
        string description,
        string[] flightsAffected,
        string[] aircraftAffected,
        int passengersAffected,
        int missedConnections,
        int expectedDelayMinutes,
        int recoveryMinutes,
        int estimatedCost,
        OperationalRisk operationalRisk,
        string[] advantages,
        string[] disadvantages,
        int score,
        int delayScore,
        int costScore,
        int passengerScore,
        int riskScore,
        DateTimeOffset createdAt)
    {
        Id = id;
        DisruptionId = disruptionId;
        Name = name;
        Action = action;
        Description = description;
        FlightsAffected = flightsAffected;
        AircraftAffected = aircraftAffected;
        PassengersAffected = passengersAffected;
        MissedConnections = missedConnections;
        ExpectedDelayMinutes = expectedDelayMinutes;
        RecoveryMinutes = recoveryMinutes;
        EstimatedCost = estimatedCost;
        OperationalRisk = operationalRisk;
        Advantages = advantages;
        Disadvantages = disadvantages;
        Score = score;
        DelayScore = delayScore;
        CostScore = costScore;
        PassengerScore = passengerScore;
        RiskScore = riskScore;
        Status = RecoveryPlanStatus.Proposed;
        CreatedAt = createdAt;
    }

    public string Id { get; private set; } = string.Empty;
    public string DisruptionId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public RecoveryActionType Action { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string[] FlightsAffected { get; private set; } = [];
    public string[] AircraftAffected { get; private set; } = [];
    public int PassengersAffected { get; private set; }
    public int MissedConnections { get; private set; }
    public int ExpectedDelayMinutes { get; private set; }
    public int RecoveryMinutes { get; private set; }
    public int EstimatedCost { get; private set; }
    public OperationalRisk OperationalRisk { get; private set; }
    public string[] Advantages { get; private set; } = [];
    public string[] Disadvantages { get; private set; } = [];
    public int Score { get; private set; }
    public bool Recommended { get; private set; }
    public int DelayScore { get; private set; }
    public int CostScore { get; private set; }
    public int PassengerScore { get; private set; }
    public int RiskScore { get; private set; }
    public RecoveryPlanStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public bool RequiresSupervisor =>
        OperationalRisk == OperationalRisk.High || EstimatedCost >= 75_000;

    public void SetRecommended(bool recommended) => Recommended = recommended;

    public bool Reject()
    {
        if (Status != RecoveryPlanStatus.Proposed)
            return false;
        Status = RecoveryPlanStatus.Rejected;
        Recommended = false;
        return true;
    }

    public bool Approve()
    {
        if (Status != RecoveryPlanStatus.Proposed)
            return false;
        Status = RecoveryPlanStatus.Approved;
        Recommended = true;
        return true;
    }
}
