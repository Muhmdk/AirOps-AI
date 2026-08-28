using AirOps.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AirOps.Api.Modules.Disruptions;

public sealed class EfDisruptionRepository(AirOpsDbContext database) : IDisruptionRepository
{
    public async Task<IReadOnlyList<Disruption>> SearchAsync(
        DisruptionStatus? status,
        DisruptionSeverity? severity,
        string? airport,
        CancellationToken cancellationToken)
    {
        IQueryable<Disruption> query = IncludeImpact(database.Disruptions).AsNoTracking();
        if (status is not null)
            query = query.Where(item => item.Status == status);
        if (severity is not null)
            query = query.Where(item => item.Severity == severity);
        if (!string.IsNullOrWhiteSpace(airport))
        {
            var normalizedAirport = airport.Trim().ToUpperInvariant();
            query = query.Where(item => item.AirportCode == normalizedAirport);
        }

        return await query.OrderByDescending(item => item.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Disruption?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        IncludeImpact(database.Disruptions)
            .SingleOrDefaultAsync(item => item.Id == id.ToUpperInvariant(), cancellationToken);

    public async Task<string> NextIdAsync(CancellationToken cancellationToken)
    {
        var latest = await database.Disruptions.Select(item => item.Id)
            .OrderByDescending(item => item)
            .FirstOrDefaultAsync(cancellationToken);
        var next = latest is not null && int.TryParse(latest.AsSpan(4), out var sequence)
            ? sequence + 1
            : 1;
        return $"DSP-{next:000}";
    }

    public async Task<IReadOnlyList<DisruptionAuditEntry>> GetAuditAsync(
        string disruptionId,
        CancellationToken cancellationToken) =>
        await database.DisruptionAuditEntries.AsNoTracking()
            .Where(item => item.DisruptionId == disruptionId.ToUpperInvariant())
            .Include(item => item.Changes)
            .OrderByDescending(item => item.Timestamp)
            .ToListAsync(cancellationToken);

    public void Add(Disruption disruption) => database.Disruptions.Add(disruption);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        database.SaveChangesAsync(cancellationToken);

    private static IQueryable<Disruption> IncludeImpact(IQueryable<Disruption> query) =>
        query.Include(item => item.Flights)
            .Include(item => item.Connections)
            .Include(item => item.GateDetails)
            .Include(item => item.CrewDetails)
            .AsSplitQuery();
}
