namespace AirOps.Api.Modules.Aircraft;

public enum AircraftStatus
{
    Available,
    InService,
    Turnaround,
    Unavailable,
}

public enum AircraftFamily
{
    Narrowbody,
    Widebody,
}

public sealed class Aircraft
{
    private Aircraft() { }

    public Aircraft(
        string registration,
        string type,
        AircraftFamily family,
        AircraftStatus status,
        string location,
        string nextFlight,
        TimeOnly? nextDeparture,
        int utilization,
        int cycles,
        decimal hours,
        int maintenanceDue,
        int health,
        int seats,
        int rangeKilometres)
    {
        Registration = registration;
        Type = type;
        Family = family;
        Status = status;
        Location = location;
        NextFlight = nextFlight;
        NextDeparture = nextDeparture;
        Utilization = utilization;
        Cycles = cycles;
        Hours = hours;
        MaintenanceDue = maintenanceDue;
        Health = health;
        Seats = seats;
        RangeKilometres = rangeKilometres;
    }

    public string Registration { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public AircraftFamily Family { get; private set; }
    public AircraftStatus Status { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public string NextFlight { get; private set; } = string.Empty;
    public TimeOnly? NextDeparture { get; private set; }
    public int Utilization { get; private set; }
    public int Cycles { get; private set; }
    public decimal Hours { get; private set; }
    public int MaintenanceDue { get; private set; }
    public int Health { get; private set; }
    public int Seats { get; private set; }
    public int RangeKilometres { get; private set; }

    public void RestoreOperationalState(
        AircraftStatus status,
        int health,
        int utilization,
        int maintenanceDue)
    {
        Status = status;
        Health = health;
        Utilization = utilization;
        MaintenanceDue = maintenanceDue;
    }

    public void ApplyDisruption(
        bool maintenance,
        bool critical,
        int durationMinutes)
    {
        Status = maintenance ? AircraftStatus.Unavailable : AircraftStatus.Turnaround;
        Health = Math.Max(35, Health - (critical ? 28 : 16));
        Utilization = Math.Max(0, Utilization - (int)Math.Round(
            durationMinutes / 10d, MidpointRounding.AwayFromZero));
        if (maintenance)
            MaintenanceDue = 0;
    }
}
