using AirOps.Api.Modules.Aircraft;
using AirOps.Api.Modules.Disruptions;
using AirOps.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirOps.Api.Modules.Recovery;

public static class RecoveryStateProjector
{
    public static async Task ProjectApprovedAsync(
        AirOpsDbContext database,
        TrackedNetworkState network,
        CancellationToken cancellationToken)
    {
        var plans = await database.RecoveryPlans.AsNoTracking()
            .Where(item => item.Status == RecoveryPlanStatus.Approved)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        if (plans.Count == 0)
            return;

        var disruptionIds = plans.Select(item => item.DisruptionId).Distinct().ToList();
        var disruptions = await database.Disruptions.AsNoTracking()
            .Where(item => disruptionIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        foreach (var plan in plans)
            if (disruptions.TryGetValue(plan.DisruptionId, out var disruption))
                Apply(network, plan, disruption);
    }

    public static void Apply(
        TrackedNetworkState network,
        RecoveryPlan plan,
        Disruption disruption)
    {
        var risk = plan.OperationalRisk switch
        {
            OperationalRisk.Low => 24,
            OperationalRisk.Medium => 42,
            _ => 58,
        };
        foreach (var flightId in plan.FlightsAffected)
        {
            var flight = network.Flights.FirstOrDefault(item => item.Id == flightId);
            flight?.ApplyRecovery(
                plan.ExpectedDelayMinutes,
                risk,
                plan.Action.ToDisplayName(),
                plan.Action == RecoveryActionType.ChangeGate);
        }

        if (plan.Action == RecoveryActionType.SwapAircraft &&
            plan.AircraftAffected.FirstOrDefault() is { } replacementRegistration &&
            plan.FlightsAffected.FirstOrDefault() is { } primaryFlight)
        {
            var original = network.Aircraft.FirstOrDefault(item =>
                item.NextFlight == primaryFlight && item.Registration != replacementRegistration);
            var replacement = network.Aircraft.FirstOrDefault(item =>
                item.Registration == replacementRegistration);
            original?.ReleaseFromFlight();
            replacement?.AssignRecoveryFlight(primaryFlight);
        }

        var airport = network.Airports.FirstOrDefault(item => item.Code == disruption.AirportCode);
        airport?.ApplyRecovery(plan.ExpectedDelayMinutes, plan.FlightsAffected.Length);
    }
}
