namespace AirOps.Api.Modules.Operations;

public enum SimulationClockStatus
{
    Paused,
    Running,
}

public sealed class SimulationClockState
{
    public const int SingletonId = 1;
    public static readonly DateTimeOffset BaselineTime =
        new DateTimeOffset(2026, 8, 6, 9, 12, 0, TimeSpan.FromHours(-4)).ToUniversalTime();

    private SimulationClockState() { }

    public SimulationClockState(DateTimeOffset updatedAt)
    {
        Id = SingletonId;
        CurrentTime = BaselineTime;
        Status = SimulationClockStatus.Paused;
        MinutesPerTick = 1;
        UpdatedAt = updatedAt;
    }

    public int Id { get; private set; }
    public DateTimeOffset CurrentTime { get; private set; }
    public SimulationClockStatus Status { get; private set; }
    public int MinutesPerTick { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Start(int minutesPerTick, DateTimeOffset updatedAt)
    {
        MinutesPerTick = minutesPerTick;
        Status = SimulationClockStatus.Running;
        UpdatedAt = updatedAt;
    }

    public void Pause(DateTimeOffset updatedAt)
    {
        Status = SimulationClockStatus.Paused;
        UpdatedAt = updatedAt;
    }

    public void Advance(int minutes, DateTimeOffset updatedAt)
    {
        CurrentTime = CurrentTime.AddMinutes(minutes);
        UpdatedAt = updatedAt;
    }

    public void Reset(DateTimeOffset updatedAt)
    {
        CurrentTime = BaselineTime;
        Status = SimulationClockStatus.Paused;
        MinutesPerTick = 1;
        UpdatedAt = updatedAt;
    }
}
