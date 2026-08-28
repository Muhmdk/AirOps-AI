namespace AirOps.Api.Modules.Disruptions;

public enum DisruptionType
{
    SevereWeather,
    AircraftMaintenance,
    LateIncomingAircraft,
    GateConflict,
    AirportCongestion,
    CrewTimingIssue,
    RunwayClosure,
    AirTrafficRestriction,
}

public enum DisruptionSeverity
{
    Moderate,
    High,
    Critical,
}

public enum DisruptionStatus
{
    Active,
    Monitoring,
    Resolved,
}

public sealed class Disruption
{
    private readonly List<ImpactedFlight> flights = [];
    private readonly List<PassengerConnectionImpact> connections = [];
    private readonly List<GateConflictImpact> gateDetails = [];
    private readonly List<CrewDutyImpact> crewDetails = [];

    private Disruption() { }

    public Disruption(
        string id,
        DisruptionType type,
        DisruptionSeverity severity,
        string airportCode,
        string primaryFlightId,
        DateTimeOffset startedAt,
        int durationMinutes,
        string description,
        DateTimeOffset createdAt,
        CalculatedNetworkImpact impact)
    {
        Id = id;
        Type = type;
        Severity = severity;
        Status = DisruptionStatus.Active;
        AirportCode = airportCode;
        PrimaryFlightId = primaryFlightId;
        StartedAt = startedAt;
        DurationMinutes = durationMinutes;
        Description = description;
        CreatedAt = createdAt;
        AffectedFlights = impact.AffectedFlights;
        AffectedPassengers = impact.AffectedPassengers;
        MissedConnections = impact.MissedConnections;
        CrewAffected = impact.CrewAffected;
        GateConflicts = impact.GateConflicts;
        HotelRooms = impact.HotelRooms;
        MealVouchers = impact.MealVouchers;
        EstimatedCompensation = impact.EstimatedCompensation;
        EstimatedOperationalCost = impact.EstimatedOperationalCost;
        RecoveryMinutes = impact.RecoveryMinutes;
        flights.AddRange(impact.Flights);
        connections.AddRange(impact.Connections);
        gateDetails.AddRange(impact.GateDetails);
        crewDetails.AddRange(impact.CrewDetails);
    }

    public string Id { get; private set; } = string.Empty;
    public DisruptionType Type { get; private set; }
    public DisruptionSeverity Severity { get; private set; }
    public DisruptionStatus Status { get; private set; }
    public string AirportCode { get; private set; } = string.Empty;
    public string PrimaryFlightId { get; private set; } = string.Empty;
    public DateTimeOffset StartedAt { get; private set; }
    public int DurationMinutes { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public int AffectedFlights { get; private set; }
    public int AffectedPassengers { get; private set; }
    public int MissedConnections { get; private set; }
    public int CrewAffected { get; private set; }
    public int GateConflicts { get; private set; }
    public int HotelRooms { get; private set; }
    public int MealVouchers { get; private set; }
    public int EstimatedCompensation { get; private set; }
    public int EstimatedOperationalCost { get; private set; }
    public int RecoveryMinutes { get; private set; }
    public IReadOnlyCollection<ImpactedFlight> Flights => flights;
    public IReadOnlyCollection<PassengerConnectionImpact> Connections => connections;
    public IReadOnlyCollection<GateConflictImpact> GateDetails => gateDetails;
    public IReadOnlyCollection<CrewDutyImpact> CrewDetails => crewDetails;

    public bool Resolve(DateTimeOffset resolvedAt)
    {
        if (Status == DisruptionStatus.Resolved)
            return false;

        Status = DisruptionStatus.Resolved;
        ResolvedAt = resolvedAt;
        return true;
    }
}

public sealed class ImpactedFlight
{
    private ImpactedFlight() { }

    public ImpactedFlight(
        string disruptionId, int sequence, string flightId, string route,
        int originalDelay, int propagatedDelay, int passengers,
        int missedConnections, string reason)
    {
        Id = Guid.NewGuid();
        DisruptionId = disruptionId;
        Sequence = sequence;
        FlightId = flightId;
        Route = route;
        OriginalDelay = originalDelay;
        PropagatedDelay = propagatedDelay;
        Passengers = passengers;
        MissedConnections = missedConnections;
        Reason = reason;
    }

    public Guid Id { get; private set; }
    public string DisruptionId { get; private set; } = string.Empty;
    public int Sequence { get; private set; }
    public string FlightId { get; private set; } = string.Empty;
    public string Route { get; private set; } = string.Empty;
    public int OriginalDelay { get; private set; }
    public int PropagatedDelay { get; private set; }
    public int Passengers { get; private set; }
    public int MissedConnections { get; private set; }
    public string Reason { get; private set; } = string.Empty;
}

public sealed class PassengerConnectionImpact
{
    private PassengerConnectionImpact() { }

    public PassengerConnectionImpact(
        string disruptionId, int sequence, string inboundFlight, string outboundFlight,
        string connectionAirport, int passengers, int minimumConnectionMinutes,
        int availableConnectionMinutes, string status)
    {
        Id = Guid.NewGuid();
        DisruptionId = disruptionId;
        Sequence = sequence;
        InboundFlight = inboundFlight;
        OutboundFlight = outboundFlight;
        ConnectionAirport = connectionAirport;
        Passengers = passengers;
        MinimumConnectionMinutes = minimumConnectionMinutes;
        AvailableConnectionMinutes = availableConnectionMinutes;
        Status = status;
    }

    public Guid Id { get; private set; }
    public string DisruptionId { get; private set; } = string.Empty;
    public int Sequence { get; private set; }
    public string InboundFlight { get; private set; } = string.Empty;
    public string OutboundFlight { get; private set; } = string.Empty;
    public string ConnectionAirport { get; private set; } = string.Empty;
    public int Passengers { get; private set; }
    public int MinimumConnectionMinutes { get; private set; }
    public int AvailableConnectionMinutes { get; private set; }
    public string Status { get; private set; } = string.Empty;
}

public sealed class GateConflictImpact
{
    private GateConflictImpact() { }

    public GateConflictImpact(
        string disruptionId, int sequence, string airport, string gate,
        string incomingFlight, string occupyingFlight, int overlapMinutes, string severity)
    {
        Id = Guid.NewGuid();
        DisruptionId = disruptionId;
        Sequence = sequence;
        Airport = airport;
        Gate = gate;
        IncomingFlight = incomingFlight;
        OccupyingFlight = occupyingFlight;
        OverlapMinutes = overlapMinutes;
        Severity = severity;
    }

    public Guid Id { get; private set; }
    public string DisruptionId { get; private set; } = string.Empty;
    public int Sequence { get; private set; }
    public string Airport { get; private set; } = string.Empty;
    public string Gate { get; private set; } = string.Empty;
    public string IncomingFlight { get; private set; } = string.Empty;
    public string OccupyingFlight { get; private set; } = string.Empty;
    public int OverlapMinutes { get; private set; }
    public string Severity { get; private set; } = string.Empty;
}

public sealed class CrewDutyImpact
{
    private CrewDutyImpact() { }

    public CrewDutyImpact(
        string disruptionId, int sequence, string crewId, string flightId, string role,
        int projectedDutyMinutes, int legalLimitMinutes, int remainingMinutes, string status)
    {
        Id = Guid.NewGuid();
        DisruptionId = disruptionId;
        Sequence = sequence;
        CrewId = crewId;
        FlightId = flightId;
        Role = role;
        ProjectedDutyMinutes = projectedDutyMinutes;
        LegalLimitMinutes = legalLimitMinutes;
        RemainingMinutes = remainingMinutes;
        Status = status;
    }

    public Guid Id { get; private set; }
    public string DisruptionId { get; private set; } = string.Empty;
    public int Sequence { get; private set; }
    public string CrewId { get; private set; } = string.Empty;
    public string FlightId { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public int ProjectedDutyMinutes { get; private set; }
    public int LegalLimitMinutes { get; private set; }
    public int RemainingMinutes { get; private set; }
    public string Status { get; private set; } = string.Empty;
}

public sealed record CalculatedNetworkImpact(
    int AffectedFlights,
    int AffectedPassengers,
    int MissedConnections,
    int CrewAffected,
    int GateConflicts,
    int HotelRooms,
    int MealVouchers,
    int EstimatedCompensation,
    int EstimatedOperationalCost,
    int RecoveryMinutes,
    IReadOnlyList<ImpactedFlight> Flights,
    IReadOnlyList<PassengerConnectionImpact> Connections,
    IReadOnlyList<GateConflictImpact> GateDetails,
    IReadOnlyList<CrewDutyImpact> CrewDetails);
