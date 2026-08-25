using AirOps.Api.Contracts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AirOps.Api.Modules.Operations;

public sealed class OperationalEventBroadcastInterceptor(
    IHubContext<OperationsHub> hub,
    ILogger<OperationalEventBroadcastInterceptor> logger) : SaveChangesInterceptor
{
    private List<OperationalEventResponse> pending = [];

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        BroadcastAsync(CancellationToken.None).GetAwaiter().GetResult();
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await BroadcastAsync(cancellationToken);
        return result;
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => pending.Clear();

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        pending.Clear();
        return Task.CompletedTask;
    }

    private void Capture(DbContext? context)
    {
        pending = context?.ChangeTracker.Entries<OperationalEvent>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => OperationalEventMappings.ToResponse(entry.Entity))
            .ToList() ?? [];
    }

    private async Task BroadcastAsync(CancellationToken cancellationToken)
    {
        var committed = pending;
        pending = [];

        foreach (var operationalEvent in committed)
        {
            try
            {
                await hub.Clients.All.SendAsync(
                    "operationalEvent", operationalEvent, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "Failed to broadcast operational event {OperationalEventId}",
                    operationalEvent.Id);
            }
        }
    }
}
