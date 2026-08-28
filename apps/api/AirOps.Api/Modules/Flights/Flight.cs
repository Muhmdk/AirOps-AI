namespace AirOps.Api.Modules.Flights;

public enum FlightStatus
{
    OnTime,
    Delayed,
    Boarding,
    AtRisk,
    Cancelled,
}

public sealed class Flight
{
    private Flight() { }

    public Flight(
        string id,
        string originCode,
        string origin,
        string destinationCode,
        string destination,
        DateTimeOffset scheduledDeparture,
        DateTimeOffset scheduledArrival,
        string aircraftRegistration,
        string aircraftType,
        string gate,
        FlightStatus status,
        int risk,
        int delayMinutes,
        int passengers,
        int connectingPassengers,
        string riskLabel)
    {
        Id = id;
        OriginCode = originCode;
        Origin = origin;
        DestinationCode = destinationCode;
        Destination = destination;
        ScheduledDeparture = scheduledDeparture;
        ScheduledArrival = scheduledArrival;
        AircraftRegistration = aircraftRegistration;
        AircraftType = aircraftType;
        Gate = gate;
        Status = status;
        Risk = risk;
        DelayMinutes = delayMinutes;
        Passengers = passengers;
        ConnectingPassengers = connectingPassengers;
        RiskLabel = riskLabel;
    }

    public string Id { get; private set; } = string.Empty;
    public string OriginCode { get; private set; } = string.Empty;
    public string Origin { get; private set; } = string.Empty;
    public string DestinationCode { get; private set; } = string.Empty;
    public string Destination { get; private set; } = string.Empty;
    public DateTimeOffset ScheduledDeparture { get; private set; }
    public DateTimeOffset ScheduledArrival { get; private set; }
    public string AircraftRegistration { get; private set; } = string.Empty;
    public string AircraftType { get; private set; } = string.Empty;
    public string Gate { get; private set; } = string.Empty;
    public FlightStatus Status { get; private set; }
    public int Risk { get; private set; }
    public int DelayMinutes { get; private set; }
    public int Passengers { get; private set; }
    public int ConnectingPassengers { get; private set; }
    public string RiskLabel { get; private set; } = string.Empty;
    public DateTimeOffset EstimatedDeparture => ScheduledDeparture.AddMinutes(DelayMinutes);
    public DateTimeOffset EstimatedArrival => ScheduledArrival.AddMinutes(DelayMinutes);
    public string Route => $"{OriginCode} → {DestinationCode}";

    public void RestoreOperationalState(
        FlightStatus status,
        int risk,
        int delayMinutes,
        string riskLabel,
        string gate)
    {
        Status = status;
        Risk = risk;
        DelayMinutes = delayMinutes;
        RiskLabel = riskLabel;
        Gate = gate;
    }

    public void ApplyDisruption(
        int propagatedDelay,
        string disruptionType,
        string baselineRiskLabel)
    {
        var overlapping = RiskLabel != baselineRiskLabel;
        var delay = Math.Max(DelayMinutes, propagatedDelay);
        var risk = Math.Max(Risk, 55 + (int)Math.Round(
            propagatedDelay / 2d, MidpointRounding.AwayFromZero));
        DelayMinutes = overlapping
            ? delay + (int)Math.Round(propagatedDelay * 0.2, MidpointRounding.AwayFromZero)
            : delay;
        Risk = Math.Min(99, risk + (overlapping ? 8 : 0));
        Status = propagatedDelay >= 30 ? FlightStatus.AtRisk : FlightStatus.Delayed;
        RiskLabel = overlapping ? $"{RiskLabel} + {disruptionType}" : disruptionType;
    }

    public void ApplyRecovery(
        int expectedDelayMinutes,
        int operationalRisk,
        string recoveryAction,
        bool changeGate)
    {
        DelayMinutes = expectedDelayMinutes;
        Risk = operationalRisk;
        Status = expectedDelayMinutes <= 15
            ? FlightStatus.OnTime
            : expectedDelayMinutes < 35
                ? FlightStatus.Delayed
                : FlightStatus.AtRisk;
        RiskLabel = $"Recovery: {recoveryAction}";
        if (changeGate)
            Gate = NextGate(Gate);
    }

    private static string NextGate(string gate)
    {
        var digits = new string(gate.SkipWhile(character => !char.IsDigit(character)).ToArray());
        if (digits.Length == 0 || !int.TryParse(digits, out var number))
            return gate;
        return $"{gate[..^digits.Length]}{number + 2}";
    }
}
