using AirOps.Api.Contracts;

namespace AirOps.Api.Modules.Airports;

public static class AirportEndpoints
{
    public static IEndpointRouteBuilder MapAirportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/airports").WithTags("Airports");
        group.MapGet("/", GetAirports).WithName("GetAirports");
        group.MapGet("/{code}", GetAirport).WithName("GetAirport");
        return endpoints;
    }

    private static async Task<IResult> GetAirports(
        IAirportRepository repository,
        string? search,
        AirportRisk? risk,
        CancellationToken cancellationToken)
    {
        var airports = await repository.SearchAsync(search, risk, cancellationToken);
        return Results.Ok(airports.Select(ToResponse));
    }

    private static async Task<IResult> GetAirport(
        string code,
        IAirportRepository repository,
        CancellationToken cancellationToken)
    {
        var airport = await repository.GetByCodeAsync(code, cancellationToken);
        return airport is null
            ? Results.NotFound(new { message = $"Airport '{code}' was not found." })
            : Results.Ok(ToResponse(airport));
    }

    private static AirportResponse ToResponse(Airport airport) => new(
        airport.Code,
        airport.Name,
        airport.City,
        airport.Province,
        airport.Timezone,
        airport.Risk.ToString(),
        airport.Health,
        airport.AverageDelay,
        airport.Departures,
        airport.Arrivals,
        airport.AtRisk,
        airport.GatesUsed,
        airport.GatesTotal,
        airport.Weather,
        airport.Temperature,
        airport.Wind,
        airport.Visibility);
}
