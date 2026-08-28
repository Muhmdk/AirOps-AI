namespace AirOps.Api.Contracts;

public sealed record PassengerJourneyResponse(
    string Id,
    string BookingReference,
    string LeadPassenger,
    int PartySize,
    string LoyaltyTier,
    string CurrentFlightId,
    string ConnectingFlightId,
    string OriginCode,
    string ConnectionAirport,
    string DestinationCode,
    int MinimumConnectionMinutes,
    int AvailableConnectionMinutes,
    int ConnectionShortfallMinutes,
    string Status,
    int RiskScore,
    IReadOnlyList<string> SpecialServices,
    IReadOnlyList<string> AlternativeFlights,
    string? SelectedAlternativeFlight,
    int EstimatedCareCost,
    string? RebookingNotes,
    DateTimeOffset UpdatedAt);

public sealed record PassengerRebookRequest(
    string AlternativeFlight,
    string Notes);
