# AirOps AI

**Airline Disruption Prediction and Recovery Platform**

AirOps AI is a full-stack airline operations command center. It gives controllers one place to monitor the network, investigate disruptions, compare recovery plans, protect passenger journeys, and record the decisions they make.

> The project currently uses deterministic risk and recovery models. Predictive machine learning is part of the roadmap and is not presented as a finished feature.

## 💡 Inspiration

A single delay rarely stays attached to one flight. Bad weather at Toronto can affect an aircraft's next rotation, create a gate conflict in Vancouver, push a crew toward its duty limit, and leave connecting passengers stranded.

The hard part is not seeing that a flight is late. It is understanding what that delay will touch next and choosing a recovery action before the problem spreads. AirOps AI was built around that problem: give an operations controller a clear view of the network and make every recovery decision traceable.

## 🔍 What it does

AirOps AI supports a complete simulated disruption-to-recovery workflow:

- Monitors network health, flights, airports, aircraft, passengers, and live operational events.
- Searches and filters flights by status, route, aircraft, and disruption risk.
- Creates operational disruptions such as severe weather, maintenance issues, crew constraints, congestion, and gate conflicts.
- Calculates affected flights, propagated delays, missed connections, crew exposure, gate conflicts, passenger impact, and estimated costs.
- Generates several recovery strategies and scores them by delay, passenger impact, cost, and operational risk.
- Lets controllers compare, approve, or reject plans with notes and supervisor authorization where required.
- Applies approved recovery actions back to the shared network state and keeps an immutable audit history.
- Identifies at-risk passenger journeys, displays service requirements and connection shortfalls, and persists rebooking decisions.
- Publishes committed operational changes to a live event timeline through SignalR.
- Includes a Scenario Lab for repeatable disruption simulations, replay, stress testing, and clean resets.

The application also has deliberate offline fallbacks, so the main workspaces remain usable when the API is unavailable. The interface clearly shows whether it is using PostgreSQL-backed data or demonstration data.

## ⚙️ How we built it

### Frontend

**Angular 22 and TypeScript:** The main web application, route-level workspaces, forms, filters, and controller workflows.

**SCSS:** Responsive dashboard layouts and reusable visual states for risk, severity, health, and recovery status.

**NgRx and RxJS:** Shared flight state, reactive data loading, live event handling, and browser fallback services.

**SignalR client:** Receives operational events after the backend transaction commits.

**Vitest and Cypress:** Unit, service-level, and browser tests for the real controller journey.

### Backend

**ASP.NET Core 9:** A modular-monolith API organized around flights, airports, aircraft, disruptions, recovery, passengers, simulation, and operations events.

**Entity Framework Core and Npgsql:** Relational persistence, migrations, repository implementations, and transactional state changes.

**PostgreSQL 17:** Stores the operational network, disruptions, recovery plans, passenger journeys, audit records, and event history.

**SignalR:** Broadcasts live events only after their related database work succeeds.

**xUnit:** Integration tests run against an in-process API host with isolated test data.

### Decision engines

**Disruption impact engine:** Deterministic rules calculate delay propagation, aircraft rotation effects, passenger connections, crew limits, gate occupancy, and cost exposure.

**Recovery scoring engine:** Produces and ranks operational choices such as maintaining the rotation, swapping aircraft, holding a connection, changing a gate, cancelling a downstream flight, or rebooking passengers.

### Local infrastructure

**Docker Compose:** Starts PostgreSQL with a persistent local volume.

**GitHub pull requests and delivery scripts:** The repository uses one branch and pull request per phase, with a separate commit for each testable feature.

## 🏗️ Architecture

```text
Angular command center
        │
        ├── REST API requests
        └── SignalR operational events
                    │
ASP.NET Core modular monolith
        ├── Flight, airport, and aircraft operations
        ├── Disruption impact engine
        ├── Recovery planning and approval
        ├── Passenger journey recovery
        ├── Simulation clock
        └── Operational audit and event stream
                    │
              PostgreSQL 17
```

## 🪦 Challenges we ran into

- Recomputing a shared network when several disruptions overlap. Resolving one event must remove only its effects while preserving every disruption that is still active.
- Keeping recovery approval transactional. A plan, network mutation, audit record, and live event cannot disagree with one another if a database operation fails.
- Supporting browser fallbacks without making offline demonstration data look like live backend data.
- Testing against persistent state. IDs and statuses change as controllers use the app, so the browser suite cannot assume that every run starts from a pristine database.
- Turning operational data into screens that are dense enough for a controller but still readable without airline-specific training.

## 😁 Accomplishments that we're proud of

- Built the full controller path from disruption creation to impact calculation, recovery comparison, approval, network mutation, and audit history.
- Connected every primary workspace to the ASP.NET Core API and PostgreSQL while preserving an offline demo mode.
- Added passenger recovery as a real workflow instead of a static metric: bookings can be searched, inspected, rebooked, and followed through the event timeline.
- Made approved recoveries restart-safe by rebuilding operational state from persisted disruptions and decisions.
- Covered the application with 36 backend integration tests, 37 Angular tests, and 14 browser journeys.
- Tested every major call to action in the UI so buttons lead to a workspace, perform an operation, or provide visible feedback.

## 📖 What we learned

Airline recovery is a state-management problem as much as it is an optimization problem. A decision that looks good for one flight can be bad for the aircraft rotation or for dozens of connecting passengers.

We also learned to treat the database transaction as the source of truth. Live updates are useful only when they describe work that has actually committed. That rule shaped the API, SignalR event flow, recovery audit, and the way the frontend refreshes state after a controller decision.

On the testing side, realistic workflows exposed problems that isolated component tests did not: stale identifiers, asynchronous filter options, database timestamp rules, and actions that appeared clickable but had no useful result.

## 🚀 Run it locally

### Prerequisites

- Node.js 24 LTS
- .NET 9 SDK
- Docker Desktop
- Corepack, included with supported Node.js releases

### 1. Start PostgreSQL and the API

From the repository root:

```bash
docker compose up -d postgres
dotnet run --project apps/api/AirOps.Api --urls http://localhost:5000
```

The API applies pending Entity Framework migrations and inserts the demonstration dataset when the relevant tables are empty.

### 2. Start the Angular application

In a second terminal:

```bash
cd apps/web-angular
corepack pnpm install
corepack pnpm start
```

Open [http://localhost:4200](http://localhost:4200).

### Demo login

```text
Controller ID: maya.chen
Password: operations
```

## 🧪 Try the main workflow

1. Open **Disruptions** and select **Trigger disruption**.
2. Choose a type, severity, airport, flight, and duration.
3. Select **Trigger and calculate impact**.
4. Review the affected rotations, passenger connections, crew, gates, and cost estimates.
5. Generate recovery plans, compare the choices, and approve or reject one with controller notes.
6. Open **Passengers** to inspect at-risk bookings and rebook a journey.
7. Open **Event timeline** to see the saved disruption, recovery, and passenger events.

## ✅ Verification

Run the repository verification script from the project root:

```bash
./scripts/verify.sh
```

With the API and web application running, execute the browser suite:

```bash
cd apps/web-angular
corepack pnpm e2e
```

Current automated coverage:

- 36 ASP.NET Core integration tests
- 37 Angular unit and service-level tests
- 14 Cypress controller journeys
- Entity Framework migration alignment check
- Angular production build

## 📁 Project structure

```text
AirOps-AI/
├── apps/
│   ├── api/
│   │   ├── AirOps.Api/          # ASP.NET Core application
│   │   └── AirOps.Api.Tests/    # Backend integration tests
│   └── web-angular/             # Angular command center and Cypress tests
├── scripts/                     # Verification, feature, and phase automation
├── compose.yaml                 # Local PostgreSQL service
└── README.md
```

## 🤔 What's next for AirOps AI

**Predictive risk models:** Train and evaluate delay and disruption models using historical flight, weather, airport, and aircraft-rotation data. Predictions will include confidence and model-version metadata rather than replacing the current deterministic rules silently.

**Live operational data:** Add adapters for airline schedules, weather feeds, airport constraints, aircraft telemetry, crew systems, and passenger reservations.

**Optimization at network scale:** Move beyond ranked heuristics toward constrained optimization across aircraft, crews, gates, passenger connections, and recovery cost.

**Production identity and permissions:** Replace the demonstration login with secure authentication, controller roles, supervisor permissions, and organization-level access controls.

**Cloud deployment and observability:** Package the services for Azure, add structured monitoring and tracing, and measure recovery outcomes over time.

**Larger simulation datasets:** Expand beyond the current Canadian demonstration network and test multi-hub disruption days with hundreds of connected flights.
