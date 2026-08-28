using AirOps.Api.Contracts;
using AirOps.Api.Modules.Disruptions;

namespace AirOps.Api.Modules.Recovery;

public static class RecoveryEndpoints
{
    public static IEndpointRouteBuilder MapRecoveryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/disruptions/{id}/recovery-plans", GetForDisruption)
            .WithName("GetRecoveryPlans").WithTags("Recovery");
        endpoints.MapPost("/api/disruptions/{id}/recovery-plans/generate", Generate)
            .WithName("GenerateRecoveryPlans").WithTags("Recovery");

        var plans = endpoints.MapGroup("/api/recovery-plans").WithTags("Recovery");
        plans.MapGet("/{id}", GetPlan).WithName("GetRecoveryPlan");
        plans.MapPost("/{id}/approve", Approve).WithName("ApproveRecoveryPlan");
        plans.MapPost("/{id}/reject", Reject).WithName("RejectRecoveryPlan");
        plans.MapGet("/{id}/audit", GetPlanAudit).WithName("GetRecoveryPlanAudit");
        endpoints.MapGet("/api/recovery-decisions", GetDecisionLog)
            .WithName("GetRecoveryDecisionLog").WithTags("Recovery");
        return endpoints;
    }

    private static async Task<IResult> GetForDisruption(
        string id,
        IDisruptionRepository disruptions,
        IRecoveryPlanRepository repository,
        CancellationToken cancellationToken)
    {
        if (await disruptions.GetByIdAsync(id, cancellationToken) is null)
            return Results.NotFound(new { message = $"Disruption '{id}' was not found." });
        var plans = await repository.GetForDisruptionAsync(id, cancellationToken);
        return Results.Ok(plans.Select(item => item.ToResponse()));
    }

    private static async Task<IResult> Generate(
        string id,
        RecoveryService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GenerateAsync(id, cancellationToken);
        if (result.Error == RecoveryOperationError.DisruptionNotFound)
            return Results.NotFound(new { message = $"Disruption '{id}' was not found." });
        if (result.Error == RecoveryOperationError.DisruptionNotActive)
            return Results.Conflict(new { message = "Recovery plans require an active disruption." });
        var response = result.Plans.Select(item => item.ToResponse()).ToList();
        return result.Created
            ? Results.Created($"/api/disruptions/{id}/recovery-plans", response)
            : Results.Ok(response);
    }

    private static async Task<IResult> GetPlan(
        string id,
        IRecoveryPlanRepository repository,
        CancellationToken cancellationToken)
    {
        var plan = await repository.GetByIdAsync(id, cancellationToken);
        return plan is null
            ? Results.NotFound(new { message = $"Recovery plan '{id}' was not found." })
            : Results.Ok(plan.ToResponse());
    }

    private static Task<IResult> Approve(
        string id,
        RecoveryDecisionRequest request,
        RecoveryService service,
        CancellationToken cancellationToken) =>
        Decide(id, request, true, service, cancellationToken);

    private static Task<IResult> Reject(
        string id,
        RecoveryDecisionRequest request,
        RecoveryService service,
        CancellationToken cancellationToken) =>
        Decide(id, request, false, service, cancellationToken);

    private static async Task<IResult> Decide(
        string id,
        RecoveryDecisionRequest request,
        bool approve,
        RecoveryService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Notes))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Notes)] = ["Decision notes are required."],
            });

        var result = approve
            ? await service.ApproveAsync(
                id, request.Notes.Trim(), request.SupervisorOverride, cancellationToken)
            : await service.RejectAsync(id, request.Notes.Trim(), cancellationToken);
        return result.Error switch
        {
            RecoveryOperationError.PlanNotFound => Results.NotFound(new
            {
                message = $"Recovery plan '{id}' was not found.",
            }),
            RecoveryOperationError.PlanNotProposed => Results.Conflict(new
            {
                message = "Only proposed recovery plans can be decided.",
            }),
            RecoveryOperationError.SupervisorRequired => Results.Conflict(new
            {
                message = "Supervisor approval is required for this plan.",
            }),
            RecoveryOperationError.DisruptionNotActive => Results.Conflict(new
            {
                message = "The disruption is no longer active.",
            }),
            RecoveryOperationError.DisruptionNotFound => Results.NotFound(new
            {
                message = "The associated disruption was not found.",
            }),
            _ => Results.Ok(new RecoveryDecisionResponse(
                result.Plan!.ToResponse(), result.Audit!.ToResponse())),
        };
    }

    private static async Task<IResult> GetPlanAudit(
        string id,
        IRecoveryPlanRepository repository,
        CancellationToken cancellationToken)
    {
        if (await repository.GetByIdAsync(id, cancellationToken) is null)
            return Results.NotFound(new { message = $"Recovery plan '{id}' was not found." });
        var entries = await repository.GetAuditAsync(id, cancellationToken);
        return Results.Ok(entries.Select(item => item.ToResponse()));
    }

    private static async Task<IResult> GetDecisionLog(
        IRecoveryPlanRepository repository,
        CancellationToken cancellationToken)
    {
        var entries = await repository.GetAuditAsync(null, cancellationToken);
        return Results.Ok(entries.Select(item => item.ToResponse()));
    }
}
