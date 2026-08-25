using System.Text.Json.Serialization;
using AirOps.Api.Modules.Aircraft;
using AirOps.Api.Modules.Airports;
using AirOps.Api.Modules.Disruptions;
using AirOps.Api.Modules.Flights;
using AirOps.Api.Modules.Network;
using AirOps.Api.Modules.Operations;
using AirOps.Api.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<AirOpsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AirOps")));
builder.Services.AddScoped<IFlightRepository, EfFlightRepository>();
builder.Services.AddScoped<IAirportRepository, EfAirportRepository>();
builder.Services.AddScoped<IAircraftRepository, EfAircraftRepository>();
builder.Services.AddScoped<IOperationalEventRepository, EfOperationalEventRepository>();
builder.Services.AddScoped<IDisruptionRepository, EfDisruptionRepository>();
builder.Services.AddScoped<DisruptionService>();
builder.Services.AddScoped<SimulationClockService>();
builder.Services.AddSingleton(TimeProvider.System);
if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHostedService<SimulationClockWorker>();
builder.Services.AddCors(options =>
    options.AddPolicy("AngularDevelopment", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();

await app.Services.InitialiseDatabaseAsync();

app.UseExceptionHandler();
app.UseCors("AngularDevelopment");

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "AirOps.Api",
    timestamp = DateTimeOffset.UtcNow,
}));
app.MapFlightEndpoints();
app.MapAirportEndpoints();
app.MapAircraftEndpoints();
app.MapNetworkEndpoints();
app.MapOperationsEndpoints();
app.MapDisruptionEndpoints();

app.Run();

public partial class Program;
