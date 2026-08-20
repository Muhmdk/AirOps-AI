# AirOps AI

AirOps AI is an airline operations control dashboard for detecting disruptions, understanding network-wide impact, and evaluating recovery actions.

## Current milestone

The browser-based core MVP (Phases 2–4) is complete in `apps/web-angular`. The application now supports the complete controller workflow: detect a disruption, calculate network impact, compare recovery options, approve an action, and inspect the resulting network outcome.

Phase 2 — Angular Operations Dashboard includes:

- Network health and operational KPI cards
- Airport risk map and live operational event feed
- Searchable high-risk flight table
- Interactive flight detail panel with risk factors and passenger impact
- Responsive desktop and mobile layouts
- Seeded Canadian airline operations data
- NgRx entity state, actions, reducer, selectors, and API-loading effect
- RxJS live operational event stream
- Lazy-loaded routes, authentication guard, and HTTP correlation interceptor
- Functional Flights workspace with reactive search, status/risk filters, sorting, and operational summaries
- Dedicated flight-detail routes with disruption risk, rotation, passenger impact, and active alerts
- Searchable Airports workspace with risk filtering, network map, weather, capacity, and traffic metrics
- Airport detail routes with affected flights and operational alerts
- Aircraft Management workspace with fleet status, type filters, utilization, health, and maintenance readiness
- Aircraft detail routes with specifications, assigned flights, and daily rotation timelines
- Live Operational Event Timeline with search, severity/category filters, pause/resume controls, and entity deep links
- Reactive controller login, protected routes, session persistence, logout, and return-URL handling
- Global error handling and HTTP correlation IDs
- Cypress end-to-end coverage for the primary controller journey

## Demo login

```text
Controller ID: maya.chen
Password: operations
```

## Run locally

Use Node.js 24 LTS or a version supported by Angular 22.

```bash
cd apps/web-angular
npm install
npm start
```

Then open `http://localhost:4200`.

## Verify

```bash
cd apps/web-angular
npm run build
npm test -- --watch=false
npm run e2e
```

The end-to-end suite expects the app to be running at `http://localhost:4200`.

## Delivery workflow

Development uses one branch and one pull request per phase. Each independently testable feature is committed separately with a Conventional Commit message.

```bash
scripts/complete-feature.sh "feat(scope): describe the feature" -- path/to/feature
scripts/complete-phase.sh "Phase N: short description"
```

The phase command runs full verification, pushes the branch, and creates or locates the GitHub pull request. See `docs/DELIVERY_WORKFLOW.md` for setup and usage.

## Phase 3 — Complete

The disruption and network-impact engine is implemented:

- Typed disruption scenarios and severity levels
- Rule-based delay calculation
- Aircraft-rotation delay propagation
- Passenger, missed-connection, crew, gate, hotel, voucher, compensation, cost, and recovery-time estimates
- Disruption creation and resolution workflow
- Disruption list and detailed network-impact visualization
- Passenger itinerary connection checks against minimum connection times
- Gate occupancy overlap detection
- Crew duty-time threshold and legal-limit monitoring
- Disruption creation and resolution events published into the live operational timeline
- Shared network-state mutation for flight delay/risk, airport health/capacity, and aircraft availability
- Baseline recomputation when disruptions are resolved
- Browser persistence for disruption and audit state across refreshes
- Controller audit history with field-level before-and-after values
- Scenario Lab with four repeatable disruption presets
- Before-and-after network snapshots, clean reset, and deterministic replay
- Simultaneous-disruption overlap detection for shared airports and aircraft rotations
- Compound delay, passenger, and cost impact plus a three-event network stress test

## Phase 4 — Complete

- Deterministic recovery candidate generation
- Six action strategies: maintain rotation, swap aircraft, hold a connection, change gate, cancel a downstream flight, and rebook passengers
- Weighted scoring across delay, passenger connections, cost, and operational risk
- Recommended-plan selection and side-by-side comparison workspace
- Explicit controller approval and rejection decisions with notes
- Supervisor thresholds, alternative rejection, and execution against network state
- Gate reassignment reflected in flight and airport operations
- Before-and-after delay, connection, and cost outcomes after execution
- Persistent recovery outcomes and immutable decision audit records
- Recovery decisions published into the live operational event stream
- Unit and Cypress coverage for the complete disruption-to-recovery journey

The Phase 4 completion condition is satisfied: a controller can select a disruption, compare at least three plans, approve or reject an option, and see the simulated network state change.

## Current verification

- 16 unit and service-level tests
- 5 Cypress end-to-end controller journeys
- Successful Angular production build

The current MVP remains a frontend simulation backed by typed in-memory services and browser persistence. The next platform milestone is the modular backend, database, simulation API, and event broker.

## Planned architecture

The repository will grow into the monorepo described in the product brief: an Angular frontend, modular API, simulation engine, ML service, shared contracts, data platform, and Azure infrastructure. The current UI uses an in-memory typed data source so it can later be replaced by NgRx effects and backend APIs without changing the interaction model.
