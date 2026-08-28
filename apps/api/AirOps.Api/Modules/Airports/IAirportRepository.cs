namespace AirOps.Api.Modules.Airports;

public interface IAirportRepository
{
    Task<IReadOnlyList<Airport>> SearchAsync(
        string? search,
        AirportRisk? risk,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Airport>> GetAllAsync(CancellationToken cancellationToken);
    Task<Airport?> GetByCodeAsync(string code, CancellationToken cancellationToken);
}
