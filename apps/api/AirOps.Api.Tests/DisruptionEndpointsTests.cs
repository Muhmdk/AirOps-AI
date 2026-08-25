using System.Net;
using System.Net.Http.Json;
using AirOps.Api.Contracts;

namespace AirOps.Api.Tests;

public sealed class DisruptionEndpointsTests
{
    [Fact]
    public async Task GetDisruptionsReturnsSeededDetailedImpact()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();

        var disruptions = await client.GetFromJsonAsync<List<DisruptionResponse>>(
            "/api/disruptions");
        var detail = await client.GetFromJsonAsync<DisruptionResponse>(
            "/api/disruptions/DSP-001");

        Assert.NotNull(disruptions);
        Assert.Equal(2, disruptions.Count);
        Assert.NotNull(detail);
        Assert.Equal("Severe weather", detail.Type);
        Assert.Equal("Critical", detail.Severity);
        Assert.Equal("YYZ", detail.Airport);
        Assert.Equal(3, detail.Impact.AffectedFlights);
        Assert.Equal(723, detail.Impact.AffectedPassengers);
        Assert.Equal(59, detail.Impact.MissedConnections);
        Assert.Equal(2, detail.Impact.GateConflicts);
        Assert.Equal(["AC103", "AC205", "AC221"],
            detail.Impact.Flights.Select(item => item.Id));
    }

    [Fact]
    public async Task GetDisruptionsSupportsOperationalFilters()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();

        var critical = await client.GetFromJsonAsync<List<DisruptionResponse>>(
            "/api/disruptions?status=Active&severity=Critical&airport=yyz");
        var invalidAirport = await client.GetAsync("/api/disruptions?airport=Toronto");

        Assert.Equal("DSP-001", Assert.Single(critical!).Id);
        Assert.Equal(HttpStatusCode.BadRequest, invalidAirport.StatusCode);
    }

    [Fact]
    public async Task CreateDisruptionPersistsImpactAndPublishesEvent()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();
        var request = new CreateDisruptionRequest(
            "Aircraft maintenance", "High", "YUL", "AC791", 90);

        var response = await client.PostAsJsonAsync("/api/disruptions", request);
        var created = await response.Content.ReadFromJsonAsync<DisruptionResponse>();
        var persisted = await client.GetFromJsonAsync<DisruptionResponse>(
            $"/api/disruptions/{created!.Id}");
        var events = await client.GetFromJsonAsync<List<OperationalEventResponse>>(
            "/api/operations/events?category=Flight");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/disruptions/{created.Id}", response.Headers.Location?.ToString());
        Assert.Equal("Aircraft maintenance", persisted!.Type);
        Assert.Equal("AC791", persisted.PrimaryFlight);
        Assert.Equal(2, persisted.Impact.AffectedFlights);
        Assert.Contains(events!, item =>
            item.Title == "Aircraft maintenance · AC791" && item.EntityId == "AC791");
    }

    [Fact]
    public async Task CreateDisruptionValidatesScenarioAndReferences()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();

        var invalid = await client.PostAsJsonAsync("/api/disruptions",
            new CreateDisruptionRequest("Unknown", "Extreme", "YY", "", 5));
        var missingFlight = await client.PostAsJsonAsync("/api/disruptions",
            new CreateDisruptionRequest("Gate conflict", "Moderate", "YYZ", "AC999", 30));
        var missingAirport = await client.PostAsJsonAsync("/api/disruptions",
            new CreateDisruptionRequest("Gate conflict", "Moderate", "ZZZ", "AC103", 30));

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingFlight.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingAirport.StatusCode);
    }

    [Fact]
    public async Task ResolveDisruptionIsPersistentAndIdempotent()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();

        var first = await client.PostAsync("/api/disruptions/DSP-002/resolve", null);
        var second = await client.PostAsync("/api/disruptions/DSP-002/resolve", null);
        var resolved = await second.Content.ReadFromJsonAsync<DisruptionResponse>();
        var events = await client.GetFromJsonAsync<List<OperationalEventResponse>>(
            "/api/operations/events?category=Flight");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal("Resolved", resolved!.Status);
        Assert.NotNull(resolved.ResolvedAt);
        Assert.Single(events!, item => item.Title == "Disruption resolved · DSP-002");
    }
}
