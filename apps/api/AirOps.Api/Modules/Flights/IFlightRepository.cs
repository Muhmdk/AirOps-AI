namespace AirOps.Api.Modules.Flights;

public interface IFlightRepository
{
    Task<IReadOnlyList<Flight>> SearchAsync(
        string? search,
        FlightStatus? status,
        int? minRisk,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Flight>> GetAllAsync(CancellationToken cancellationToken);
    Task<Flight?> GetByIdAsync(string id, CancellationToken cancellationToken);
}
