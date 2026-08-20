using System.Net;
using System.Net.Http.Json;
using AirOps.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AirOps.Api.Tests;

public sealed class FlightEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public FlightEndpointsTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetFlightsReturnsRiskOrderedOperation()
    {
        var flights = await client.GetFromJsonAsync<List<FlightResponse>>("/api/flights");

        Assert.NotNull(flights);
        Assert.Equal(5, flights.Count);
        Assert.Equal("AC103", flights[0].Id);
        Assert.Equal("YYZ → YVR", flights[0].Route);
        Assert.True(flights.Zip(flights.Skip(1)).All(pair => pair.First.Risk >= pair.Second.Risk));
    }

    [Fact]
    public async Task GetFlightsSupportsOperationalFilters()
    {
        var flights = await client.GetFromJsonAsync<List<FlightResponse>>(
            "/api/flights?search=YYZ&minRisk=70");

        Assert.NotNull(flights);
        Assert.Equal(2, flights.Count);
        Assert.All(flights, flight => Assert.True(flight.Risk >= 70));
    }

    [Fact]
    public async Task GetFlightReturnsDetailOrNotFound()
    {
        var flight = await client.GetFromJsonAsync<FlightResponse>("/api/flights/ac882");
        var missing = await client.GetAsync("/api/flights/AC999");

        Assert.NotNull(flight);
        Assert.Equal("C-FITL", flight.AircraftRegistration);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task GetNetworkSummaryAggregatesSeededFlights()
    {
        var summary = await client.GetFromJsonAsync<NetworkSummaryResponse>(
            "/api/network/summary");

        Assert.NotNull(summary);
        Assert.Equal(5, summary.FlightsToday);
        Assert.Equal(2, summary.AtRisk);
        Assert.Equal(2, summary.HighRisk);
        Assert.Equal(1178, summary.Passengers);
        Assert.InRange(summary.NetworkHealth, 0, 100);
    }

    [Fact]
    public async Task HealthEndpointReportsServiceReadiness()
    {
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
