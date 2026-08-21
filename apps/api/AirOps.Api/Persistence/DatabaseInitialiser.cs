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

        if (await database.Flights.AnyAsync())
            return;

        database.Flights.AddRange(FlightSeed.All);
        await database.SaveChangesAsync();
    }
}
