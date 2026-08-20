# AirOps API

The AirOps backend is an ASP.NET Core modular monolith. The first module exposes seeded flight operations and a network summary through HTTP APIs while keeping persistence behind repository interfaces.

## Run

```bash
dotnet run --project apps/api/AirOps.Api
```

The development API listens on the URL printed by ASP.NET Core. Available endpoints:

- `GET /health`
- `GET /api/flights`
- `GET /api/flights/{id}`
- `GET /api/network/summary`

Flight list query parameters:

- `search`: flight number, airport code, or city
- `status`: `OnTime`, `Delayed`, `Boarding`, `AtRisk`, or `Cancelled`
- `minRisk`: inclusive risk threshold from 0 to 100

## Test

```bash
dotnet test apps/api/AirOps.Api.Tests/AirOps.Api.Tests.csproj
```

PostgreSQL will replace `SeededFlightRepository` in the next backend feature without changing endpoint contracts.
