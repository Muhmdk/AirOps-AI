using System.Text.Json.Serialization;
using AirOps.Api.Modules.Flights;
using AirOps.Api.Modules.Network;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IFlightRepository, SeededFlightRepository>();
builder.Services.AddCors(options =>
    options.AddPolicy("AngularDevelopment", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("AngularDevelopment");

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "AirOps.Api",
    timestamp = DateTimeOffset.UtcNow,
}));
app.MapFlightEndpoints();
app.MapNetworkEndpoints();

app.Run();

public partial class Program;
