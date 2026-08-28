using AirOps.Api.Modules.Disruptions;
using AirOps.Api.Modules.Operations;
using AirOps.Api.Modules.Recovery;
using Microsoft.EntityFrameworkCore;

namespace AirOps.Api.Persistence;

public static class DatabaseInitialiser
{
    public static async Task InitialiseDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AirOpsDbContext>();

        if (database.Database.IsRelational())
            await database.Database.MigrateAsync();
        else
            await database.Database.EnsureCreatedAsync();

        if (!await database.Flights.AnyAsync())
            database.Flights.AddRange(FlightSeed.All);
        if (!await database.Airports.AnyAsync())
            database.Airports.AddRange(AirportSeed.All);
        if (!await database.Aircraft.AnyAsync())
            database.Aircraft.AddRange(AircraftSeed.All);
        if (!await database.OperationalEvents.AnyAsync())
            database.OperationalEvents.AddRange(OperationalEventSeed.All);
        if (!await database.SimulationClocks.AnyAsync())
            database.SimulationClocks.Add(new SimulationClockState(DateTimeOffset.UtcNow));
        if (!await database.Disruptions.AnyAsync())
            database.Disruptions.AddRange(DisruptionSeed.All);
        if (!await database.PassengerJourneys.AnyAsync())
            database.PassengerJourneys.AddRange(PassengerSeed.All);
        await database.SaveChangesAsync();

        var activeDisruptions = await database.Disruptions.AsNoTracking()
            .Where(item => item.Status == DisruptionStatus.Active)
            .Include(item => item.Flights)
            .Include(item => item.Connections)
            .Include(item => item.GateDetails)
            .Include(item => item.CrewDetails)
            .AsSplitQuery()
            .OrderByDescending(item => item.StartedAt)
            .ToListAsync();
        var network = await NetworkStateProjector.LoadAsync(database, CancellationToken.None);
        NetworkStateProjector.Project(network, activeDisruptions);
        await RecoveryStateProjector.ProjectApprovedAsync(
            database, network, CancellationToken.None);
        await database.SaveChangesAsync();
    }
}
