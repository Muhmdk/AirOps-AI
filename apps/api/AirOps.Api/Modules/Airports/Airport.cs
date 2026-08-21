namespace AirOps.Api.Modules.Airports;

public enum AirportRisk
{
    Low,
    Moderate,
    High,
}

public sealed class Airport
{
    private Airport() { }

    public Airport(
        string code,
        string name,
        string city,
        string province,
        string timezone,
        AirportRisk risk,
        int health,
        int averageDelay,
        int departures,
        int arrivals,
        int atRisk,
        int gatesUsed,
        int gatesTotal,
        string weather,
        int temperature,
        string wind,
        string visibility)
    {
        Code = code;
        Name = name;
        City = city;
        Province = province;
        Timezone = timezone;
        Risk = risk;
        Health = health;
        AverageDelay = averageDelay;
        Departures = departures;
        Arrivals = arrivals;
        AtRisk = atRisk;
        GatesUsed = gatesUsed;
        GatesTotal = gatesTotal;
        Weather = weather;
        Temperature = temperature;
        Wind = wind;
        Visibility = visibility;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string Province { get; private set; } = string.Empty;
    public string Timezone { get; private set; } = string.Empty;
    public AirportRisk Risk { get; private set; }
    public int Health { get; private set; }
    public int AverageDelay { get; private set; }
    public int Departures { get; private set; }
    public int Arrivals { get; private set; }
    public int AtRisk { get; private set; }
    public int GatesUsed { get; private set; }
    public int GatesTotal { get; private set; }
    public string Weather { get; private set; } = string.Empty;
    public int Temperature { get; private set; }
    public string Wind { get; private set; } = string.Empty;
    public string Visibility { get; private set; } = string.Empty;
}
