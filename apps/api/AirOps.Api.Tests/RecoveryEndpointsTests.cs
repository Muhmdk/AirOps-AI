using System.Net;
using System.Net.Http.Json;
using AirOps.Api.Contracts;
using AirOps.Api.Persistence;

namespace AirOps.Api.Tests;

public sealed class RecoveryEndpointsTests
{
    [Fact]
    public async Task GenerateCreatesRankedPersistentCandidatesOnce()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();

        var empty = await client.GetFromJsonAsync<List<RecoveryPlanResponse>>(
            "/api/disruptions/DSP-001/recovery-plans");
        var firstResponse = await client.PostAsync(
            "/api/disruptions/DSP-001/recovery-plans/generate", null);
        var first = await firstResponse.Content.ReadFromJsonAsync<List<RecoveryPlanResponse>>();
        var secondResponse = await client.PostAsync(
            "/api/disruptions/DSP-001/recovery-plans/generate", null);
        var second = await secondResponse.Content.ReadFromJsonAsync<List<RecoveryPlanResponse>>();

        Assert.Empty(empty!);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(6, first!.Count);
        Assert.Equal(first.Select(item => item.Id), second!.Select(item => item.Id));
        Assert.True(first.Zip(first.Skip(1)).All(pair => pair.First.Score >= pair.Second.Score));
        Assert.Single(first, item => item.Recommended);
        var swap = Assert.Single(first, item => item.Action == "Swap aircraft");
        Assert.Equal(["C-FITL"], swap.AircraftAffected);
        Assert.Equal(["AC103", "AC205", "AC221"], swap.FlightsAffected);
        Assert.Equal(31, swap.ExpectedDelayMinutes);
        Assert.True(swap.ScoreBreakdown.Risk > 0);
    }

    [Fact]
    public async Task RejectRecordsDecisionAndPromotesNextCandidate()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();
        var plans = await GenerateAsync(client, "DSP-001");
        var recommended = Assert.Single(plans, item => item.Recommended);

        var response = await client.PostAsJsonAsync(
            $"/api/recovery-plans/{recommended.Id}/reject",
            new RecoveryDecisionRequest("Connection protection is insufficient."));
        var decision = await response.Content.ReadFromJsonAsync<RecoveryDecisionResponse>();
        var current = await client.GetFromJsonAsync<List<RecoveryPlanResponse>>(
            "/api/disruptions/DSP-001/recovery-plans");
        var disruption = await client.GetFromJsonAsync<DisruptionResponse>(
            "/api/disruptions/DSP-001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Rejected", decision!.Plan.Status);
        Assert.Equal("Rejected", decision.Audit.Action);
        Assert.Equal(decision.Audit.Outcome.DelayBefore, decision.Audit.Outcome.DelayAfter);
        Assert.Equal("Active", disruption!.Status);
        Assert.Single(current!, item => item.Recommended && item.Status == "Proposed");
    }

    [Fact]
    public async Task SwapApprovalExecutesAndSurvivesStateReinitialization()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();
        var plans = await GenerateAsync(client, "DSP-002");
        var swap = Assert.Single(plans, item => item.Action == "Swap aircraft");

        var response = await client.PostAsJsonAsync(
            $"/api/recovery-plans/{swap.Id}/approve",
            new RecoveryDecisionRequest("Protect the downstream rotation."));
        var decision = await response.Content.ReadFromJsonAsync<RecoveryDecisionResponse>();
        await factory.Services.InitialiseDatabaseAsync();
        var flight = await client.GetFromJsonAsync<FlightResponse>("/api/flights/AC418");
        var original = await client.GetFromJsonAsync<AircraftResponse>("/api/aircraft/C-GROV");
        var replacement = await client.GetFromJsonAsync<AircraftResponse>("/api/aircraft/C-FITL");
        var disruption = await client.GetFromJsonAsync<DisruptionResponse>(
            "/api/disruptions/DSP-002");
        var current = await client.GetFromJsonAsync<List<RecoveryPlanResponse>>(
            "/api/disruptions/DSP-002/recovery-plans");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Approved", decision!.Plan.Status);
        Assert.Equal("Maya Chen", decision.Audit.Actor);
        Assert.Equal(15, flight!.DelayMinutes);
        Assert.Equal(24, flight.Risk);
        Assert.Equal("Recovery: Swap aircraft", flight.RiskLabel);
        Assert.Equal("Available", original!.Status);
        Assert.Equal("Unassigned", original.NextFlight);
        Assert.Equal("In service", replacement!.Status);
        Assert.Equal("AC418", replacement.NextFlight);
        Assert.Equal("Resolved", disruption!.Status);
        Assert.Single(current!, item => item.Status == "Approved");
        Assert.Equal(5, current!.Count(item => item.Status == "Rejected"));
    }

    [Fact]
    public async Task HighRiskApprovalRequiresAndRecordsSupervisorAuthorization()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();
        var plans = await GenerateAsync(client, "DSP-002");
        var cancellation = Assert.Single(
            plans, item => item.Action == "Cancel downstream flight");

        var denied = await client.PostAsJsonAsync(
            $"/api/recovery-plans/{cancellation.Id}/approve",
            new RecoveryDecisionRequest("Break the delayed rotation."));
        var approved = await client.PostAsJsonAsync(
            $"/api/recovery-plans/{cancellation.Id}/approve",
            new RecoveryDecisionRequest("Supervisor authorizes cancellation.", true));
        var decision = await approved.Content.ReadFromJsonAsync<RecoveryDecisionResponse>();
        var log = await client.GetFromJsonAsync<List<RecoveryAuditResponse>>(
            "/api/recovery-decisions");

        Assert.Equal(HttpStatusCode.Conflict, denied.StatusCode);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        Assert.True(decision!.Audit.SupervisorOverride);
        Assert.Equal("Alex Morgan", decision.Audit.Actor);
        Assert.Equal("Operations Supervisor", decision.Audit.ActorRole);
        Assert.Contains(log!, item => item.PlanId == cancellation.Id && item.Action == "Approved");
    }

    [Fact]
    public async Task DecisionsValidateNotesAndPlanState()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();
        var plans = await GenerateAsync(client, "DSP-002");
        var plan = plans.First(item => !item.RequiresSupervisor);

        var missingNotes = await client.PostAsJsonAsync(
            $"/api/recovery-plans/{plan.Id}/approve", new RecoveryDecisionRequest(""));
        var first = await client.PostAsJsonAsync(
            $"/api/recovery-plans/{plan.Id}/approve", new RecoveryDecisionRequest("Proceed."));
        var repeated = await client.PostAsJsonAsync(
            $"/api/recovery-plans/{plan.Id}/approve", new RecoveryDecisionRequest("Again."));
        var missing = await client.GetAsync("/api/recovery-plans/RCP-999-1/audit");

        Assert.Equal(HttpStatusCode.BadRequest, missingNotes.StatusCode);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    private static async Task<List<RecoveryPlanResponse>> GenerateAsync(
        HttpClient client,
        string disruptionId)
    {
        var response = await client.PostAsync(
            $"/api/disruptions/{disruptionId}/recovery-plans/generate", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<RecoveryPlanResponse>>())!;
    }
}
