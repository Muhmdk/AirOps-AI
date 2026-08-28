namespace AirOps.Api.Modules.Aircraft;

public interface IAircraftRepository
{
    Task<IReadOnlyList<Aircraft>> SearchAsync(
        string? search,
        AircraftStatus? status,
        AircraftFamily? family,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Aircraft>> GetAllAsync(CancellationToken cancellationToken);
    Task<Aircraft?> GetByRegistrationAsync(
        string registration,
        CancellationToken cancellationToken);
}
