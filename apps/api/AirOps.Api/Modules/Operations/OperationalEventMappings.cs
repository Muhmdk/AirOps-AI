using System.Globalization;
using AirOps.Api.Contracts;

namespace AirOps.Api.Modules.Operations;

internal static class OperationalEventMappings
{
    private static readonly TimeSpan DemoOffset = TimeSpan.FromHours(-4);

    internal static OperationalEventResponse ToResponse(OperationalEvent item) => new(
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
}
