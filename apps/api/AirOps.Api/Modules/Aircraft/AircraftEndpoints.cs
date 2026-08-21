using System.Globalization;
using AirOps.Api.Contracts;

namespace AirOps.Api.Modules.Aircraft;

public static class AircraftEndpoints
{
    public static IEndpointRouteBuilder MapAircraftEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/aircraft").WithTags("Aircraft");
        group.MapGet("/", GetAircraft).WithName("GetAircraftCollection");
        group.MapGet("/{registration}", GetAircraftByRegistration).WithName("GetAircraft");
        return endpoints;
    }

    private static async Task<IResult> GetAircraft(
        IAircraftRepository repository,
        string? search,
        AircraftStatus? status,
        AircraftFamily? family,
        CancellationToken cancellationToken)
    {
        var aircraft = await repository.SearchAsync(
            search, status, family, cancellationToken);
        return Results.Ok(aircraft.Select(ToResponse));
    }

    private static async Task<IResult> GetAircraftByRegistration(
        string registration,
        IAircraftRepository repository,
        CancellationToken cancellationToken)
    {
        var aircraft = await repository.GetByRegistrationAsync(registration, cancellationToken);
        return aircraft is null
            ? Results.NotFound(new { message = $"Aircraft '{registration}' was not found." })
            : Results.Ok(ToResponse(aircraft));
    }

    private static AircraftResponse ToResponse(Aircraft aircraft) => new(
        aircraft.Registration,
        aircraft.Type,
        aircraft.Family.ToString(),
        aircraft.Status switch
        {
            AircraftStatus.InService => "In service",
            _ => aircraft.Status.ToString(),
        },
        aircraft.Location,
        aircraft.NextFlight,
        aircraft.NextDeparture?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "—",
        aircraft.Utilization,
        aircraft.Cycles,
        aircraft.Hours,
        aircraft.MaintenanceDue,
        aircraft.Health,
        aircraft.Seats,
        $"{aircraft.RangeKilometres.ToString("N0", CultureInfo.InvariantCulture)} km");
}
