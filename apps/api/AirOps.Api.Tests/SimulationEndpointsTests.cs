using System.Net;
using System.Net.Http.Json;
using AirOps.Api.Contracts;

namespace AirOps.Api.Tests;

public sealed class SimulationEndpointsTests : IClassFixture<AirOpsApiFactory>
{
    private readonly HttpClient client;

    public SimulationEndpointsTests(AirOpsApiFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetEventsReturnsPersistentHistory()
    {
        await ResetAsync();
        var events = await client.GetFromJsonAsync<List<OperationalEventResponse>>(
            "/api/operations/events");

        Assert.NotNull(events);
        Assert.Equal(6, events.Count);
        Assert.Equal("Weather risk raised", events[0].Title);
        Assert.Equal("09:08", events[0].Time);
    }

    [Fact]
    public async Task GetEventsSupportsSeverityAndCategoryFilters()
    {
        await ResetAsync();
        var critical = await client.GetFromJsonAsync<List<OperationalEventResponse>>(
            "/api/operations/events?severity=Critical");
        var aircraft = await client.GetFromJsonAsync<List<OperationalEventResponse>>(
            "/api/operations/events?category=Aircraft");

        Assert.Equal(2, critical!.Count);
        Assert.Equal("C-GJYE", Assert.Single(aircraft!).EntityId);
    }

    [Fact]
    public async Task ManualAdvanceGeneratesFlightMilestoneOnce()
    {
        var baseline = await ResetAsync();
        var advance = await client.PostAsJsonAsync(
            "/api/simulation/clock/advance", new AdvanceSimulationRequest(5));
        var clock = await advance.Content.ReadFromJsonAsync<SimulationClockResponse>();
        var events = await client.GetFromJsonAsync<List<OperationalEventResponse>>(
            "/api/operations/events?category=Flight");

        Assert.Equal(HttpStatusCode.OK, advance.StatusCode);
        Assert.NotNull(clock);
        Assert.Equal(baseline.CurrentTime.AddMinutes(5), clock.CurrentTime);
        Assert.Contains(events!, item => item.Title == "AC103 departed");

        await client.PostAsJsonAsync(
            "/api/simulation/clock/advance", new AdvanceSimulationRequest(1));
        var repeatedEvents = await client.GetFromJsonAsync<List<OperationalEventResponse>>(
            "/api/operations/events?category=Flight");
        Assert.Single(repeatedEvents!, item => item.Title == "AC103 departed");
    }

    [Fact]
    public async Task ClockSupportsStartPauseAndValidation()
    {
        await ResetAsync();
        var invalid = await client.PostAsJsonAsync(
            "/api/simulation/clock/start", new StartSimulationRequest(0));
        var started = await client.PostAsJsonAsync(
            "/api/simulation/clock/start", new StartSimulationRequest(3));
        var running = await started.Content.ReadFromJsonAsync<SimulationClockResponse>();
        var pausedResponse = await client.PostAsync("/api/simulation/clock/pause", null);
        var paused = await pausedResponse.Content.ReadFromJsonAsync<SimulationClockResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("Running", running!.Status);
        Assert.Equal(3, running.MinutesPerTick);
        Assert.Equal("Paused", paused!.Status);
    }

    [Fact]
    public async Task EventLimitIsValidated()
    {
        var response = await client.GetAsync("/api/operations/events?limit=201");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<SimulationClockResponse> ResetAsync()
    {
        var response = await client.PostAsync("/api/simulation/clock/reset", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SimulationClockResponse>())!;
    }
}
