using AirOps.Api.Contracts;

namespace AirOps.Api.Modules.Flights;

internal static class FlightMappings
{
    internal static FlightResponse ToResponse(this Flight flight) => new(
        flight.Id,
        flight.Route,
        flight.OriginCode,
        flight.Origin,
        flight.DestinationCode,
        flight.Destination,
        flight.ScheduledDeparture,
        flight.EstimatedDeparture,
        flight.ScheduledArrival,
        flight.EstimatedArrival,
        flight.AircraftRegistration,
        flight.AircraftType,
        flight.Gate,
        flight.Status.ToString(),
        flight.Risk,
        flight.DelayMinutes,
        flight.Passengers,
        flight.ConnectingPassengers,
        flight.RiskLabel);
}
