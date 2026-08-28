using AirOps.Api.Contracts;
using AirOps.Api.Modules.Aircraft;
using AirOps.Api.Modules.Airports;
using AirOps.Api.Modules.Flights;

namespace AirOps.Api.Modules.Network;

public static class NetworkEndpoints
{
    public static IEndpointRouteBuilder MapNetworkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/network/summary", GetSummary)
            .WithName("GetNetworkSummary")
            .WithTags("Network");
        return endpoints;
    }

    private static async Task<NetworkSummaryResponse> GetSummary(
        IFlightRepository repository,
        IAirportRepository airportRepository,
        IAircraftRepository aircraftRepository,
        CancellationToken cancellationToken)
    {
        var flights = await repository.GetAllAsync(cancellationToken);
        var airports = await airportRepository.GetAllAsync(cancellationToken);
        var aircraft = await aircraftRepository.GetAllAsync(cancellationToken);
        var networkHealth = airports.Count == 0
            ? 100
            : (int)Math.Round(airports.Average(airport => airport.Health));
        return new NetworkSummaryResponse(
            FlightsToday: flights.Count,
            OnTime: flights.Count(flight => flight.Status == FlightStatus.OnTime),
            Delayed: flights.Count(flight => flight.Status == FlightStatus.Delayed),
            Boarding: flights.Count(flight => flight.Status == FlightStatus.Boarding),
            AtRisk: flights.Count(flight => flight.Status == FlightStatus.AtRisk),
            Cancelled: flights.Count(flight => flight.Status == FlightStatus.Cancelled),
            HighRisk: flights.Count(flight => flight.Risk >= 70),
            Passengers: flights.Sum(flight => flight.Passengers),
            ConnectingPassengers: flights.Sum(flight => flight.ConnectingPassengers),
            NetworkHealth: Math.Clamp(networkHealth, 0, 100),
            AirportsMonitored: airports.Count,
            AirportAverageDelay: airports.Count == 0
                ? 0
                : (int)Math.Round(airports.Average(airport => airport.AverageDelay)),
            AircraftAvailable: aircraft.Count(item => item.Status != AircraftStatus.Unavailable),
            AircraftUnavailable: aircraft.Count(item => item.Status == AircraftStatus.Unavailable));
    }
}
