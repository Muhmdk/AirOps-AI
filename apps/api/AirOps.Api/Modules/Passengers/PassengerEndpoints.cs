using AirOps.Api.Contracts;

namespace AirOps.Api.Modules.Passengers;

public static class PassengerEndpoints
{
    public static IEndpointRouteBuilder MapPassengerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/passengers").WithTags("Passengers");
        group.MapGet("/", GetPassengers).WithName("GetPassengers");
        group.MapGet("/{id}", GetPassenger).WithName("GetPassenger");
        group.MapPost("/{id}/rebook", Rebook).WithName("RebookPassenger");
        return endpoints;
    }

    private static async Task<IResult> GetPassengers(
        IPassengerJourneyRepository repository,
        string? search,
        PassengerJourneyStatus? status,
        string? flightId,
        CancellationToken cancellationToken)
    {
        var journeys = await repository.SearchAsync(
            search, status, flightId, cancellationToken);
        return Results.Ok(journeys.Select(item => item.ToResponse()));
    }

    private static async Task<IResult> GetPassenger(
        string id,
        IPassengerJourneyRepository repository,
        CancellationToken cancellationToken)
    {
        var journey = await repository.GetByIdAsync(id, false, cancellationToken);
        return journey is null
            ? Results.NotFound(new { message = $"Passenger journey '{id}' was not found." })
            : Results.Ok(journey.ToResponse());
    }

    private static async Task<IResult> Rebook(
        string id,
        PassengerRebookRequest request,
        PassengerService service,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.AlternativeFlight))
            errors[nameof(request.AlternativeFlight)] = ["An alternate itinerary is required."];
        if (string.IsNullOrWhiteSpace(request.Notes))
            errors[nameof(request.Notes)] = ["Controller notes are required."];
        else if (request.Notes.Trim().Length < 12)
            errors[nameof(request.Notes)] = ["Controller notes must contain at least 12 characters."];
        else if (request.Notes.Trim().Length > 500)
            errors[nameof(request.Notes)] = ["Controller notes cannot exceed 500 characters."];
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var result = await service.RebookAsync(
            id, request.AlternativeFlight, request.Notes, cancellationToken);
        return result.Error switch
        {
            PassengerRebookError.NotFound => Results.NotFound(new
            {
                message = $"Passenger journey '{id}' was not found.",
            }),
            PassengerRebookError.AlreadyRebooked => Results.Conflict(new
            {
                message = "This passenger journey has already been rebooked.",
            }),
            PassengerRebookError.InvalidAlternative => Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [nameof(request.AlternativeFlight)] = ["Select an available alternate itinerary."],
                }),
            _ => Results.Ok(result.Journey!.ToResponse()),
        };
    }
}
