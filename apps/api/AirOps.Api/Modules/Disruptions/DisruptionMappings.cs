using AirOps.Api.Contracts;

namespace AirOps.Api.Modules.Disruptions;

internal static class DisruptionMappings
{
    internal static DisruptionResponse ToResponse(this Disruption disruption) => new(
        disruption.Id,
        disruption.Type.ToDisplayName(),
        disruption.Severity.ToString(),
        disruption.Status.ToString(),
        disruption.AirportCode,
        disruption.PrimaryFlightId,
        disruption.StartedAt,
        disruption.DurationMinutes,
        disruption.Description,
        new NetworkImpactResponse(
            disruption.AffectedFlights,
            disruption.AffectedPassengers,
            disruption.MissedConnections,
            disruption.CrewAffected,
            disruption.GateConflicts,
            disruption.HotelRooms,
            disruption.MealVouchers,
            disruption.EstimatedCompensation,
            disruption.EstimatedOperationalCost,
            disruption.RecoveryMinutes,
            disruption.Flights.OrderBy(item => item.Sequence)
                .Select(item => new ImpactedFlightResponse(
                    item.FlightId, item.Route, item.OriginalDelay, item.PropagatedDelay,
                    item.Passengers, item.MissedConnections, item.Reason)).ToList(),
            disruption.Connections.OrderBy(item => item.Sequence)
                .Select(item => new PassengerConnectionImpactResponse(
                    item.InboundFlight, item.OutboundFlight, item.ConnectionAirport,
                    item.Passengers, item.MinimumConnectionMinutes,
                    item.AvailableConnectionMinutes, item.Status)).ToList(),
            disruption.GateDetails.OrderBy(item => item.Sequence)
                .Select(item => new GateConflictImpactResponse(
                    item.Airport, item.Gate, item.IncomingFlight, item.OccupyingFlight,
                    item.OverlapMinutes, item.Severity)).ToList(),
            disruption.CrewDetails.OrderBy(item => item.Sequence)
                .Select(item => new CrewDutyImpactResponse(
                    item.CrewId, item.FlightId, item.Role, item.ProjectedDutyMinutes,
                    item.LegalLimitMinutes, item.RemainingMinutes, item.Status)).ToList()),
        disruption.CreatedAt,
        disruption.ResolvedAt);

    internal static DisruptionAuditResponse ToResponse(this DisruptionAuditEntry entry) => new(
        entry.Id,
        entry.DisruptionId,
        entry.Action.ToString(),
        entry.Actor,
        entry.Timestamp,
        entry.Summary,
        entry.Changes.Select(item => new NetworkMutationResponse(
            item.EntityType,
            item.EntityId,
            item.Field,
            item.BeforeValue,
            item.AfterValue)).ToList());
}
