namespace AirOps.Api.Modules.Operations;

public sealed class SimulationClockWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SimulationClockWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var clock = scope.ServiceProvider.GetRequiredService<SimulationClockService>();
                await clock.TickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Simulation clock tick failed");
            }
        }
    }
}
