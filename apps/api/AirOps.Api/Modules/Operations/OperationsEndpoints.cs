using System.Globalization;
using AirOps.Api.Contracts;

namespace AirOps.Api.Modules.Operations;

public static class OperationsEndpoints
{
    private static readonly TimeSpan DemoOffset = TimeSpan.FromHours(-4);

    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/operations/events", GetEvents)
            .WithName("GetOperationalEvents")
            .WithTags("Operations");

        var clock = endpoints.MapGroup("/api/simulation/clock").WithTags("Simulation");
        clock.MapGet("/", GetClock).WithName("GetSimulationClock");
        clock.MapPost("/start", StartClock).WithName("StartSimulationClock");
        clock.MapPost("/pause", PauseClock).WithName("PauseSimulationClock");
        clock.MapPost("/advance", AdvanceClock).WithName("AdvanceSimulationClock");
        clock.MapPost("/reset", ResetClock).WithName("ResetSimulationClock");
        return endpoints;
    }

    private static async Task<IResult> GetEvents(
        IOperationalEventRepository repository,
        OperationalEventSeverity? severity,
        OperationalEventCategory? category,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 200)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(limit)] = ["Limit must be between 1 and 200."],
            });

        var events = await repository.SearchAsync(
            severity, category, limit, cancellationToken);
        return Results.Ok(events.Select(ToResponse));
    }

    private static async Task<SimulationClockResponse> GetClock(
        SimulationClockService service,
        CancellationToken cancellationToken) =>
        ToResponse(await service.GetAsync(cancellationToken));

    private static async Task<IResult> StartClock(
        StartSimulationRequest request,
        SimulationClockService service,
        CancellationToken cancellationToken)
    {
        if (request.MinutesPerTick is < 1 or > 60)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.MinutesPerTick)] = ["Minutes per tick must be between 1 and 60."],
            });
        return Results.Ok(ToResponse(await service.StartAsync(
            request.MinutesPerTick, cancellationToken)));
    }

    private static async Task<SimulationClockResponse> PauseClock(
        SimulationClockService service,
        CancellationToken cancellationToken) =>
        ToResponse(await service.PauseAsync(cancellationToken));

    private static async Task<IResult> AdvanceClock(
        AdvanceSimulationRequest request,
        SimulationClockService service,
        CancellationToken cancellationToken)
    {
        if (request.Minutes is < 1 or > 1440)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Minutes)] = ["Advance minutes must be between 1 and 1440."],
            });
        return Results.Ok(ToResponse(await service.AdvanceAsync(
            request.Minutes, cancellationToken)));
    }

    private static async Task<SimulationClockResponse> ResetClock(
        SimulationClockService service,
        CancellationToken cancellationToken) =>
        ToResponse(await service.ResetAsync(cancellationToken));

    private static OperationalEventResponse ToResponse(OperationalEvent item) => new(
        item.Id,
        item.OccurredAt,
        item.OccurredAt.ToOffset(DemoOffset).ToString("HH:mm", CultureInfo.InvariantCulture),
        item.Type.ToString().ToLowerInvariant(),
        item.Title,
        item.Detail,
        item.Accent,
        item.Severity.ToString(),
        item.EntityType,
        item.EntityId,
        item.Category.ToString());

    private static SimulationClockResponse ToResponse(SimulationClockState clock) => new(
        clock.CurrentTime,
        clock.Status.ToString(),
        clock.MinutesPerTick,
        clock.UpdatedAt);
}
