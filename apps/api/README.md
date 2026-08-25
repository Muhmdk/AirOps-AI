# AirOps API

The AirOps backend is an ASP.NET Core modular monolith. The first module exposes seeded flight operations and a network summary through HTTP APIs while keeping persistence behind repository interfaces.

## Run

```bash
dotnet run --project apps/api/AirOps.Api --urls http://localhost:5000
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
- `GET /api/operations/events`
- `GET /api/simulation/clock`
- `POST /api/simulation/clock/start`
- `POST /api/simulation/clock/pause`
- `POST /api/simulation/clock/advance`
- `POST /api/simulation/clock/reset`
- `GET /api/disruptions`
- `GET /api/disruptions/{id}`
- `POST /api/disruptions`
- `POST /api/disruptions/{id}/resolve`
- `GET /api/disruptions/{id}/audit`
- `GET /api/disruptions/{id}/recovery-plans`
- `POST /api/disruptions/{id}/recovery-plans/generate`
- `GET /api/recovery-plans/{id}`
- `POST /api/recovery-plans/{id}/approve`
- `POST /api/recovery-plans/{id}/reject`
- `GET /api/recovery-plans/{id}/audit`
- `GET /api/recovery-decisions`
- `WS /hubs/operations` (`operationalEvent` SignalR messages)

Flight list query parameters:

- `search`: flight number, airport code, or city
- `status`: `OnTime`, `Delayed`, `Boarding`, `AtRisk`, or `Cancelled`
- `minRisk`: inclusive risk threshold from 0 to 100

Airport queries support `search` and `risk`. Aircraft queries support `search`, `status`, and `family`.

Operational events support `severity`, `category`, and `limit` filters. The simulation clock starts paused at the deterministic demonstration time. Manual advancement and the five-second background ticker publish flight-departure milestones exactly once.

Every operational event is broadcast through the SignalR hub only after its database transaction commits. Clients can load retained history through the REST endpoint, subscribe for live updates, and safely reconnect without relying on an in-memory broker.

Disruption queries support `status`, `severity`, and `airport`. Creating a disruption validates its flight and airport, persists detailed aircraft-rotation, passenger-connection, gate-conflict, crew-duty, cost, and recovery impacts, and publishes an operational event. Resolving a disruption is idempotent and records one resolution event.

Active disruptions are projected into persistent flight, airport, and aircraft operational state. Every creation or resolution recomputes the state from the deterministic baseline and all remaining active disruptions, preserving overlapping effects. Field-level before-and-after mutations are stored in the disruption audit trail.

Recovery generation creates up to six scored strategies for an active disruption and returns the persisted candidates on subsequent calls. Decisions require notes; high-risk or high-cost plans require supervisor authorization. Approval rejects competing candidates, resolves the disruption, applies the selected recovery to persistent network state, publishes an event, and records an immutable outcome audit. Approved outcomes are reapplied during startup projection.

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
