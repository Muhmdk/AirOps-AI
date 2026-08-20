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

    private static IResult GetFlights(
        IFlightRepository repository,
        string? search,
        FlightStatus? status,
        int? minRisk)
    {
        if (minRisk is < 0 or > 100)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(minRisk)] = ["Minimum risk must be between 0 and 100."],
            });

        IEnumerable<Flight> flights = repository.GetAll();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            flights = flights.Where(flight =>
                flight.Id.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                flight.OriginCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                flight.DestinationCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                flight.Origin.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                flight.Destination.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (status is not null)
            flights = flights.Where(flight => flight.Status == status);
        if (minRisk is not null)
            flights = flights.Where(flight => flight.Risk >= minRisk);

        return Results.Ok(flights
            .OrderByDescending(flight => flight.Risk)
            .Select(flight => flight.ToResponse()));
    }

    private static IResult GetFlight(string id, IFlightRepository repository)
    {
        var flight = repository.GetById(id);
        return flight is null
            ? Results.NotFound(new { message = $"Flight '{id}' was not found." })
            : Results.Ok(flight.ToResponse());
    }
}
