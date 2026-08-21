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
- `GET /api/airports`
- `GET /api/airports/{code}`
- `GET /api/aircraft`
- `GET /api/aircraft/{registration}`
- `GET /api/network/summary`

Flight list query parameters:

- `search`: flight number, airport code, or city
- `status`: `OnTime`, `Delayed`, `Boarding`, `AtRisk`, or `Cancelled`
- `minRisk`: inclusive risk threshold from 0 to 100

Airport queries support `search` and `risk`. Aircraft queries support `search`, `status`, and `family`.

## Test

```bash
dotnet test apps/api/AirOps.Api.Tests/AirOps.Api.Tests.csproj
```

## PostgreSQL

Start the local database:

```bash
docker compose up -d postgres
dotnet run --project apps/api/AirOps.Api
```

PostgreSQL is published on host port `5433` to avoid colliding with a default local PostgreSQL installation.

The API applies pending Entity Framework migrations at startup and inserts the deterministic five-flight dataset only when the table is empty. Override the development connection with the `ConnectionStrings__AirOps` environment variable.

Restore the repository-local Entity Framework tool before creating future migrations:

```bash
dotnet tool restore
dotnet ef migrations add MigrationName --project apps/api/AirOps.Api
```

```bash
docker compose down
```

Add `-v` only when you intentionally want to remove the persisted development database.
