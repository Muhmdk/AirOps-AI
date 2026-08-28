using System.Net.Http.Json;
using AirOps.Api.Contracts;

namespace AirOps.Api.Tests;

public sealed class DisruptionNetworkStateTests
{
    [Fact]
    public async Task SeededActiveDisruptionsAreProjectedIntoNetworkState()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();

        var flight = await client.GetFromJsonAsync<FlightResponse>("/api/flights/AC103");
        var delayed = await client.GetFromJsonAsync<FlightResponse>("/api/flights/AC418");
        var airport = await client.GetFromJsonAsync<AirportResponse>("/api/airports/YYZ");
        var aircraft = await client.GetFromJsonAsync<AircraftResponse>("/api/aircraft/C-FVLX");

        Assert.Equal(91, flight!.DelayMinutes);
        Assert.Equal(99, flight.Risk);
        Assert.Equal(45, delayed!.DelayMinutes);
        Assert.Equal(78, delayed.Risk);
        Assert.Equal(51, airport!.Health);
        Assert.Equal(17, airport.AtRisk);
        Assert.Equal(61, airport.GatesUsed);
        Assert.Equal("Severe thunderstorms", airport.Weather);
        Assert.Equal("Turnaround", aircraft!.Status);
        Assert.Equal(66, aircraft.Health);
        Assert.Equal(75, aircraft.Utilization);
    }

    [Fact]
    public async Task CreateMutatesPersistentNetworkAndRecordsFieldAudit()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/disruptions",
            new CreateDisruptionRequest("Gate conflict", "Moderate", "YYZ", "AC882", 60));
        var disruption = await create.Content.ReadFromJsonAsync<DisruptionResponse>();
        var flight = await client.GetFromJsonAsync<FlightResponse>("/api/flights/AC882");
        var aircraft = await client.GetFromJsonAsync<AircraftResponse>("/api/aircraft/C-FITL");
        var audit = await client.GetFromJsonAsync<List<DisruptionAuditResponse>>(
            $"/api/disruptions/{disruption!.Id}/audit");

        Assert.Equal("Delayed", flight!.Status);
        Assert.Equal(18, flight.DelayMinutes);
        Assert.Equal(64, flight.Risk);
        Assert.Equal("Turnaround", aircraft!.Status);
        Assert.Equal(81, aircraft.Health);
        var entry = Assert.Single(audit!);
        Assert.Equal("Created", entry.Action);
        Assert.Equal("Maya Chen", entry.Actor);
        Assert.Contains(entry.Changes, change =>
            change is { EntityType: "Flight", EntityId: "AC882", Field: "Delay", Before: "0", After: "18" });
        Assert.Contains(entry.Changes, change =>
            change is { EntityType: "Aircraft", EntityId: "C-FITL", Field: "Status", Before: "Available", After: "Turnaround" });
    }

    [Fact]
    public async Task ResolveRecomputesRemainingOverlapsAndRecordsRollbackAudit()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();

        var firstResponse = await client.PostAsJsonAsync("/api/disruptions",
            new CreateDisruptionRequest("Gate conflict", "Moderate", "YYZ", "AC882", 60));
        var first = await firstResponse.Content.ReadFromJsonAsync<DisruptionResponse>();
        var firstState = await client.GetFromJsonAsync<FlightResponse>("/api/flights/AC882");
        var secondResponse = await client.PostAsJsonAsync("/api/disruptions",
            new CreateDisruptionRequest("Aircraft maintenance", "High", "YYZ", "AC882", 90));
        var second = await secondResponse.Content.ReadFromJsonAsync<DisruptionResponse>();

        await client.PostAsync($"/api/disruptions/{second!.Id}/resolve", null);
        var recomputed = await client.GetFromJsonAsync<FlightResponse>("/api/flights/AC882");
        var aircraft = await client.GetFromJsonAsync<AircraftResponse>("/api/aircraft/C-FITL");
        var audit = await client.GetFromJsonAsync<List<DisruptionAuditResponse>>(
            $"/api/disruptions/{second.Id}/audit");
        var remaining = await client.GetFromJsonAsync<DisruptionResponse>(
            $"/api/disruptions/{first!.Id}");

        Assert.Equal("Active", remaining!.Status);
        Assert.Equal(firstState!.DelayMinutes, recomputed!.DelayMinutes);
        Assert.Equal(firstState.Risk, recomputed.Risk);
        Assert.Equal(firstState.RiskLabel, recomputed.RiskLabel);
        Assert.Equal("Turnaround", aircraft!.Status);
        Assert.Equal(2, audit!.Count);
        var resolved = Assert.Single(audit, item => item.Action == "Resolved");
        Assert.Contains(resolved.Changes, change =>
            change is { EntityType: "Aircraft", EntityId: "C-FITL", Field: "Status", Before: "Unavailable", After: "Turnaround" });
    }

    [Fact]
    public async Task AuditForMissingDisruptionReturnsNotFound()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/disruptions/DSP-999/audit");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
