using System.Net;
using System.Net.Http.Json;
using AirOps.Api.Contracts;

namespace AirOps.Api.Tests;

public sealed class OperationsEndpointsTests : IClassFixture<AirOpsApiFactory>
{
    private readonly HttpClient client;

    public OperationsEndpointsTests(AirOpsApiFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAirportsReturnsSeededNetwork()
    {
        var airports = await client.GetFromJsonAsync<List<AirportResponse>>("/api/airports");

        Assert.NotNull(airports);
        Assert.Equal(6, airports.Count);
        Assert.Contains(airports, airport => airport.Code == "YYZ" && airport.Risk == "High");
        Assert.All(airports, airport => Assert.InRange(airport.GatesUsed, 0, airport.GatesTotal));
    }

    [Fact]
    public async Task GetAirportsSupportsSearchAndRiskFilters()
    {
        var airports = await client.GetFromJsonAsync<List<AirportResponse>>(
            "/api/airports?search=Toronto&risk=High");

        var airport = Assert.Single(airports!);
        Assert.Equal("YYZ", airport.Code);
    }

    [Fact]
    public async Task GetAirportReturnsDetailOrNotFound()
    {
        var airport = await client.GetFromJsonAsync<AirportResponse>("/api/airports/yyz");
        var missing = await client.GetAsync("/api/airports/ZZZ");

        Assert.NotNull(airport);
        Assert.Equal("Toronto Pearson International", airport.Name);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task GetAircraftReturnsSeededFleet()
    {
        var fleet = await client.GetFromJsonAsync<List<AircraftResponse>>("/api/aircraft");

        Assert.NotNull(fleet);
        Assert.Equal(6, fleet.Count);
        Assert.Contains(fleet, aircraft =>
            aircraft.Registration == "C-FVLX" && aircraft.Status == "Turnaround");
    }

    [Fact]
    public async Task GetAircraftSupportsStatusAndFamilyFilters()
    {
        var unavailable = await client.GetFromJsonAsync<List<AircraftResponse>>(
            "/api/aircraft?status=Unavailable");
        var widebody = await client.GetFromJsonAsync<List<AircraftResponse>>(
            "/api/aircraft?family=Widebody");

        Assert.Equal("C-GJYE", Assert.Single(unavailable!).Registration);
        Assert.Equal(3, widebody!.Count);
    }

    [Fact]
    public async Task GetAircraftReturnsDetailOrNotFound()
    {
        var aircraft = await client.GetFromJsonAsync<AircraftResponse>(
            "/api/aircraft/c-fvlx");
        var missing = await client.GetAsync("/api/aircraft/C-XXXX");

        Assert.NotNull(aircraft);
        Assert.Equal("14,140 km", aircraft.Range);
        Assert.Equal("09:15", aircraft.NextDeparture);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
