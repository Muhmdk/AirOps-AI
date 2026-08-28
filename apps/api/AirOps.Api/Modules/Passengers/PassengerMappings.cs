using AirOps.Api.Contracts;

namespace AirOps.Api.Modules.Passengers;

internal static class PassengerMappings
{
    internal static PassengerJourneyResponse ToResponse(this PassengerJourney item) => new(
        item.Id,
        item.BookingReference,
        item.LeadPassenger,
        item.PartySize,
        item.LoyaltyTier,
        item.CurrentFlightId,
        item.ConnectingFlightId,
        item.OriginCode,
        item.ConnectionAirport,
        item.DestinationCode,
        item.MinimumConnectionMinutes,
        item.AvailableConnectionMinutes,
        item.ConnectionShortfallMinutes,
        item.Status switch
        {
            PassengerJourneyStatus.AtRisk => "At risk",
            _ => item.Status.ToString(),
        },
        item.RiskScore,
        item.SpecialServices,
        item.AlternativeFlights,
        item.SelectedAlternativeFlight,
        item.EstimatedCareCost,
        item.RebookingNotes,
        item.UpdatedAt);
}
