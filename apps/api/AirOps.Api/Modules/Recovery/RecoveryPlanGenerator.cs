using AirOps.Api.Modules.Disruptions;

namespace AirOps.Api.Modules.Recovery;

public static class RecoveryPlanGenerator
{
    private static readonly IReadOnlyList<RecoveryCandidate> Candidates =
    [
        new(RecoveryActionType.MaintainRotation, "Maintain current rotation",
            "Keep the assigned aircraft and absorb the propagated delay.",
            1, 1, 1, 1, OperationalRisk.Medium,
            ["No aircraft or crew reassignment", "Lowest implementation complexity"],
            ["Longest passenger delay", "Delay continues through downstream rotation"]),
        new(RecoveryActionType.SwapAircraft, "Swap with available aircraft",
            "Assign a compatible available aircraft and protect the original rotation.",
            0.34, 0.62, 0.24, 0.46, OperationalRisk.Low,
            ["Reduces downstream delay", "Protects most passenger connections"],
            ["Requires compatible spare aircraft", "Creates an aircraft repositioning requirement"]),
        new(RecoveryActionType.HoldConnectingFlight, "Protect passenger connections",
            "Hold the highest-value outbound connection while the disrupted flight arrives.",
            0.72, 0.78, 0.38, 0.82, OperationalRisk.Medium,
            ["Protects connecting passengers", "Avoids large-scale rebooking"],
            ["Transfers delay to another flight", "May create another gate conflict"]),
        new(RecoveryActionType.ChangeGate, "Reassign to compatible gate",
            "Move the primary flight to a nearby compatible gate and remove the occupancy conflict.",
            0.48, 0.56, 0.62, 0.52, OperationalRisk.Low,
            ["Clears the active gate conflict", "Requires no aircraft reassignment"],
            ["Requires passenger and ramp-team movement", "Gate compatibility must be reconfirmed"]),
        new(RecoveryActionType.CancelDownstreamFlight, "Cancel lowest-impact downstream leg",
            "Break the affected rotation by cancelling its lowest-demand downstream service.",
            0.22, 1.18, 0.55, 0.38, OperationalRisk.High,
            ["Stops rotation delay propagation", "Restores aircraft schedule quickly"],
            ["Requires passenger reaccommodation", "Highest customer-service impact"]),
        new(RecoveryActionType.RebookPassengers, "Proactive passenger rebooking",
            "Keep the operation unchanged while moving at-risk connections to alternatives.",
            0.9, 0.86, 0.16, 0.92, OperationalRisk.Low,
            ["Minimizes missed connections", "Can begin before flight arrival"],
            ["Does not improve aircraft rotation", "Consumes available seat inventory"]),
    ];

    public static IReadOnlyList<RecoveryPlan> Generate(
        Disruption disruption,
        string? availableAircraft,
        DateTimeOffset createdAt)
    {
        var maxDelay = disruption.Flights.Max(item => item.PropagatedDelay);
        var sequence = disruption.Id.AsSpan(4).ToString();
        var plans = Candidates
            .Where(item => item.Action != RecoveryActionType.SwapAircraft ||
                availableAircraft is not null)
            .Select((candidate, index) =>
            {
                var delay = Math.Max(8, Round(maxDelay * candidate.DelayFactor));
                var cost = Round(
                    disruption.EstimatedOperationalCost * candidate.CostFactor +
                    (candidate.Action == RecoveryActionType.SwapAircraft ? 6_500 : 0));
                var missed = Round(disruption.MissedConnections * candidate.ConnectionFactor);
                var recovery = Round(disruption.RecoveryMinutes * candidate.RecoveryFactor);
                var delayScore = InverseScore(delay, 140);
                var costScore = InverseScore(cost, 160_000);
                var passengerScore = InverseScore(missed, 80);
                var riskScore = candidate.Risk switch
                {
                    OperationalRisk.Low => 95,
                    OperationalRisk.Medium => 70,
                    _ => 38,
                };
                var score = Round(
                    delayScore * 0.3 + costScore * 0.25 +
                    passengerScore * 0.3 + riskScore * 0.15);
                return new RecoveryPlan(
                    $"RCP-{sequence}-{index + 1}",
                    disruption.Id,
                    candidate.Name,
                    candidate.Action,
                    candidate.Description,
                    disruption.Flights.OrderBy(item => item.Sequence)
                        .Select(item => item.FlightId).ToArray(),
                    candidate.Action == RecoveryActionType.SwapAircraft
                        ? [availableAircraft!]
                        : [],
                    disruption.AffectedPassengers,
                    missed,
                    delay,
                    recovery,
                    cost,
                    candidate.Risk,
                    candidate.Advantages,
                    candidate.Disadvantages,
                    score,
                    delayScore,
                    costScore,
                    passengerScore,
                    riskScore,
                    createdAt);
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Id)
            .ToList();
        plans.FirstOrDefault()?.SetRecommended(true);
        return plans;
    }

    private static int InverseScore(int value, int worst) =>
        Math.Max(0, Round(100 - value / (double)worst * 100));

    private static int Round(double value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private sealed record RecoveryCandidate(
        RecoveryActionType Action,
        string Name,
        string Description,
        double DelayFactor,
        double CostFactor,
        double ConnectionFactor,
        double RecoveryFactor,
        OperationalRisk Risk,
        string[] Advantages,
        string[] Disadvantages);
}
