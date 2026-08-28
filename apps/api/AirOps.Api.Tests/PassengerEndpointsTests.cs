using System.Net;
using System.Net.Http.Json;
using AirOps.Api.Contracts;

namespace AirOps.Api.Tests;

public sealed class PassengerEndpointsTests
{
    [Fact]
    public async Task GetPassengersReturnsRiskOrderedJourneysAndSupportsFilters()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();

        var all = await client.GetFromJsonAsync<List<PassengerJourneyResponse>>(
            "/api/passengers");
        var atRisk = await client.GetFromJsonAsync<List<PassengerJourneyResponse>>(
            "/api/passengers?status=AtRisk&flightId=AC103");
        var search = await client.GetFromJsonAsync<List<PassengerJourneyResponse>>(
            "/api/passengers?search=Rahman");

        Assert.Equal(6, all!.Count);
        Assert.Equal(13, all.Sum(item => item.PartySize));
        Assert.Equal("PAX-003", all[0].Id);
        Assert.True(all.Zip(all.Skip(1)).All(pair =>
            pair.First.RiskScore >= pair.Second.RiskScore));
        Assert.Single(atRisk!, item => item.Id == "PAX-001");
        Assert.Single(search!, item => item.BookingReference == "7Q4K2M");
    }

    [Fact]
    public async Task GetPassengerReturnsCompleteJourneyOrNotFound()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();

        var journey = await client.GetFromJsonAsync<PassengerJourneyResponse>(
            "/api/passengers/pax-001");
        var missing = await client.GetAsync("/api/passengers/PAX-999");

        Assert.NotNull(journey);
        Assert.Equal("AC103", journey.CurrentFlightId);
        Assert.Equal("AC205", journey.ConnectingFlightId);
        Assert.Equal(29, journey.ConnectionShortfallMinutes);
        Assert.Equal(2, journey.AlternativeFlights.Count);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task RebookPersistsJourneyAndPublishesPassengerEvent()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();
        const string alternative = "AC612 · YYZ → YHZ · 12:35";

        var response = await client.PostAsJsonAsync(
            "/api/passengers/PAX-002/rebook",
            new PassengerRebookRequest(
                alternative,
                "Protect the party on a direct service before the connection closes."));
        var updated = await response.Content.ReadFromJsonAsync<PassengerJourneyResponse>();
        var persisted = await client.GetFromJsonAsync<PassengerJourneyResponse>(
            "/api/passengers/PAX-002");
        var events = await client.GetFromJsonAsync<List<OperationalEventResponse>>(
            "/api/operations/events?category=Passenger&limit=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Rebooked", updated!.Status);
        Assert.Equal(18, updated.RiskScore);
        Assert.Equal(alternative, persisted!.SelectedAlternativeFlight);
        Assert.Contains(events!, item =>
            item.EntityType == "passenger" && item.EntityId == "PAX-002" &&
            item.Title.Contains("rebooked"));
    }

    [Fact]
    public async Task RebookValidatesInputAlternativeAndJourneyState()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();

        var missingNotes = await client.PostAsJsonAsync(
            "/api/passengers/PAX-001/rebook",
            new PassengerRebookRequest("AC125 · YYZ → YYC · 12:10", ""));
        var shortNotes = await client.PostAsJsonAsync(
            "/api/passengers/PAX-001/rebook",
            new PassengerRebookRequest("AC125 · YYZ → YYC · 12:10", "Too short"));
        var invalidAlternative = await client.PostAsJsonAsync(
            "/api/passengers/PAX-001/rebook",
            new PassengerRebookRequest("AC999 · Invalid", "Try an invalid service."));
        var alreadyRebooked = await client.PostAsJsonAsync(
            "/api/passengers/PAX-006/rebook",
            new PassengerRebookRequest(
                "AC125 · YYZ → YYC · 12:10", "Attempt a duplicate rebooking."));

        Assert.Equal(HttpStatusCode.BadRequest, missingNotes.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, shortNotes.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidAlternative.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, alreadyRebooked.StatusCode);
    }
}
