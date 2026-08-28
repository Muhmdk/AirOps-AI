using System.Net.Http.Json;
using AirOps.Api.Contracts;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace AirOps.Api.Tests;

public sealed class OperationsHubTests
{
    [Fact]
    public async Task CommittedOperationalEventIsBroadcastToConnectedClients()
    {
        await using var factory = new AirOpsApiFactory();
        using var client = factory.CreateClient();
        var received = new TaskCompletionSource<OperationalEventResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(client.BaseAddress!, "/hubs/operations"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
        connection.On<OperationalEventResponse>("operationalEvent", received.SetResult);
        await connection.StartAsync();

        var response = await client.PostAsJsonAsync("/api/disruptions",
            new CreateDisruptionRequest(
                "Aircraft maintenance", "High", "YUL", "AC791", 90));
        response.EnsureSuccessStatusCode();

        var operationalEvent = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("Aircraft maintenance · AC791", operationalEvent.Title);
        Assert.Equal("AC791", operationalEvent.EntityId);
        Assert.Equal("Flight", operationalEvent.Category);
    }
}
