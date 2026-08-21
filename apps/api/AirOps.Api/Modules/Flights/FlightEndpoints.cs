using AirOps.Api.Contracts;

namespace AirOps.Api.Modules.Flights;

public static class FlightEndpoints
{
    public static IEndpointRouteBuilder MapFlightEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/flights").WithTags("Flights");

        group.MapGet("/", GetFlights).WithName("GetFlights");
        group.MapGet("/{id}", GetFlight).WithName("GetFlight");

        return endpoints;
    }

    private static async Task<IResult> GetFlights(
        IFlightRepository repository,
        string? search,
        FlightStatus? status,
        int? minRisk,
        CancellationToken cancellationToken)
    {
        if (minRisk is < 0 or > 100)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(minRisk)] = ["Minimum risk must be between 0 and 100."],
            });

        var flights = await repository.SearchAsync(
            search, status, minRisk, cancellationToken);
        return Results.Ok(flights.Select(flight => flight.ToResponse()));
    }

    private static async Task<IResult> GetFlight(
        string id,
        IFlightRepository repository,
        CancellationToken cancellationToken)
    {
        var flight = await repository.GetByIdAsync(id, cancellationToken);
        return flight is null
            ? Results.NotFound(new { message = $"Flight '{id}' was not found." })
            : Results.Ok(flight.ToResponse());
    }
}
