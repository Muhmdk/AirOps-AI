namespace AirOps.Api.Modules.Flights;

public interface IFlightRepository
{
    IReadOnlyList<Flight> GetAll();
    Flight? GetById(string id);
}
