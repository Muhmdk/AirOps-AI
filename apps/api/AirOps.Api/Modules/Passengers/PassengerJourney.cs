namespace AirOps.Api.Modules.Passengers;

public enum PassengerJourneyStatus
{
    Protected,
    AtRisk,
    Misconnected,
    Rebooked,
}

public sealed class PassengerJourney
{
    private PassengerJourney() { }

    public PassengerJourney(
        string id,
        string bookingReference,
        string leadPassenger,
        int partySize,
        string loyaltyTier,
        string currentFlightId,
        string connectingFlightId,
        string originCode,
        string connectionAirport,
        string destinationCode,
        int minimumConnectionMinutes,
        int availableConnectionMinutes,
        PassengerJourneyStatus status,
        int riskScore,
        string[] specialServices,
        string[] alternativeFlights,
        int estimatedCareCost,
        DateTimeOffset updatedAt,
        string? selectedAlternativeFlight = null,
        string? rebookingNotes = null)
    {
        Id = id;
        BookingReference = bookingReference;
        LeadPassenger = leadPassenger;
        PartySize = partySize;
        LoyaltyTier = loyaltyTier;
        CurrentFlightId = currentFlightId;
        ConnectingFlightId = connectingFlightId;
        OriginCode = originCode;
        ConnectionAirport = connectionAirport;
        DestinationCode = destinationCode;
        MinimumConnectionMinutes = minimumConnectionMinutes;
        AvailableConnectionMinutes = availableConnectionMinutes;
        Status = status;
        RiskScore = riskScore;
        SpecialServices = specialServices;
        AlternativeFlights = alternativeFlights;
        EstimatedCareCost = estimatedCareCost;
        UpdatedAt = updatedAt;
        SelectedAlternativeFlight = selectedAlternativeFlight;
        RebookingNotes = rebookingNotes;
    }

    public string Id { get; private set; } = string.Empty;
    public string BookingReference { get; private set; } = string.Empty;
    public string LeadPassenger { get; private set; } = string.Empty;
    public int PartySize { get; private set; }
    public string LoyaltyTier { get; private set; } = string.Empty;
    public string CurrentFlightId { get; private set; } = string.Empty;
    public string ConnectingFlightId { get; private set; } = string.Empty;
    public string OriginCode { get; private set; } = string.Empty;
    public string ConnectionAirport { get; private set; } = string.Empty;
    public string DestinationCode { get; private set; } = string.Empty;
    public int MinimumConnectionMinutes { get; private set; }
    public int AvailableConnectionMinutes { get; private set; }
    public PassengerJourneyStatus Status { get; private set; }
    public int RiskScore { get; private set; }
    public string[] SpecialServices { get; private set; } = [];
    public string[] AlternativeFlights { get; private set; } = [];
    public string? SelectedAlternativeFlight { get; private set; }
    public int EstimatedCareCost { get; private set; }
    public string? RebookingNotes { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int ConnectionShortfallMinutes =>
        Math.Max(0, MinimumConnectionMinutes - AvailableConnectionMinutes);

    public bool Rebook(string alternativeFlight, string notes, DateTimeOffset updatedAt)
    {
        if (Status == PassengerJourneyStatus.Rebooked)
            return false;
        SelectedAlternativeFlight = alternativeFlight;
        RebookingNotes = notes;
        Status = PassengerJourneyStatus.Rebooked;
        RiskScore = Math.Min(RiskScore, 18);
        UpdatedAt = updatedAt;
        return true;
    }
}
