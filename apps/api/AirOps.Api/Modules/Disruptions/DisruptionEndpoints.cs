using AirOps.Api.Contracts;

namespace AirOps.Api.Modules.Disruptions;

public static class DisruptionEndpoints
{
    public static IEndpointRouteBuilder MapDisruptionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/disruptions").WithTags("Disruptions");
        group.MapGet("/", GetDisruptions).WithName("GetDisruptions");
        group.MapGet("/{id}", GetDisruption).WithName("GetDisruption");
        group.MapGet("/{id}/audit", GetDisruptionAudit).WithName("GetDisruptionAudit");
        group.MapPost("/", CreateDisruption).WithName("CreateDisruption");
        group.MapPost("/{id}/resolve", ResolveDisruption).WithName("ResolveDisruption");
        return endpoints;
    }

    private static async Task<IResult> GetDisruptions(
        IDisruptionRepository repository,
        DisruptionStatus? status,
        DisruptionSeverity? severity,
        string? airport,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(airport) && airport.Trim().Length != 3)
            return Validation(nameof(airport), "Airport must be a three-letter IATA code.");

        var disruptions = await repository.SearchAsync(
            status, severity, airport, cancellationToken);
        return Results.Ok(disruptions.Select(item => item.ToResponse()));
    }

    private static async Task<IResult> GetDisruption(
        string id,
        IDisruptionRepository repository,
        CancellationToken cancellationToken)
    {
        var disruption = await repository.GetByIdAsync(id, cancellationToken);
        return disruption is null
            ? Results.NotFound(new { message = $"Disruption '{id}' was not found." })
            : Results.Ok(disruption.ToResponse());
    }

    private static async Task<IResult> GetDisruptionAudit(
        string id,
        IDisruptionRepository repository,
        CancellationToken cancellationToken)
    {
        var disruption = await repository.GetByIdAsync(id, cancellationToken);
        if (disruption is null)
            return Results.NotFound(new { message = $"Disruption '{id}' was not found." });

        var entries = await repository.GetAuditAsync(id, cancellationToken);
        return Results.Ok(entries.Select(item => item.ToResponse()));
    }

    private static async Task<IResult> CreateDisruption(
        CreateDisruptionRequest request,
        DisruptionService service,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (!DisruptionTypeNames.TryParse(request.Type, out var type))
            errors[nameof(request.Type)] = ["Disruption type is not supported."];
        if (!Enum.TryParse<DisruptionSeverity>(request.Severity, true, out var severity))
            errors[nameof(request.Severity)] = ["Severity must be Moderate, High, or Critical."];
        if (string.IsNullOrWhiteSpace(request.Airport) || request.Airport.Trim().Length != 3)
            errors[nameof(request.Airport)] = ["Airport must be a three-letter IATA code."];
        if (string.IsNullOrWhiteSpace(request.FlightId))
            errors[nameof(request.FlightId)] = ["Flight ID is required."];
        if (request.DurationMinutes is < 15 or > 1440)
            errors[nameof(request.DurationMinutes)] = ["Duration must be between 15 and 1440 minutes."];
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var result = await service.CreateAsync(
            type,
            severity,
            request.Airport.Trim().ToUpperInvariant(),
            request.FlightId.Trim().ToUpperInvariant(),
            request.DurationMinutes,
            cancellationToken);
        return result.Error switch
        {
            DisruptionCreationError.FlightNotFound => Results.NotFound(new
            {
                message = $"Flight '{request.FlightId}' was not found.",
            }),
            DisruptionCreationError.AirportNotFound => Results.NotFound(new
            {
                message = $"Airport '{request.Airport}' was not found.",
            }),
            _ => Results.Created($"/api/disruptions/{result.Disruption!.Id}",
                result.Disruption.ToResponse()),
        };
    }

    private static async Task<IResult> ResolveDisruption(
        string id,
        DisruptionService service,
        CancellationToken cancellationToken)
    {
        var disruption = await service.ResolveAsync(id, cancellationToken);
        return disruption is null
            ? Results.NotFound(new { message = $"Disruption '{id}' was not found." })
            : Results.Ok(disruption.ToResponse());
    }

    private static IResult Validation(string field, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
