using AirOps.Api.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AirOps.Api.Tests;

public sealed class AirOpsApiFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"AirOpsTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AirOpsDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AirOpsDbContext>>();
            services.AddDbContext<AirOpsDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        });
    }
}
