using AirOps.Api.Contracts;
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

    private static NetworkSummaryResponse GetSummary(IFlightRepository repository)
    {
        var flights = repository.GetAll();
        var averageRisk = flights.Count == 0 ? 0 : flights.Average(flight => flight.Risk);
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
            NetworkHealth: Math.Clamp((int)Math.Round(100 - averageRisk * 0.42), 0, 100));
    }
}
