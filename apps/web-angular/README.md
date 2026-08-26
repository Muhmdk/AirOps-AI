# WebAngular

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 22.1.3.

## Development server

Start the API from the repository root:

```bash
docker compose up -d postgres
dotnet run --project apps/api/AirOps.Api --urls http://localhost:5000
```

Then start Angular in another terminal:

```bash
cd apps/web-angular
corepack pnpm install
corepack pnpm start
```

Open `http://localhost:4200/`. The development proxy sends REST and SignalR traffic to the API on port 5000. Flight, airport, aircraft, and disruption workflows, overview network metrics, and the event timeline use the backend. If the API is unavailable, the affected screens retain seeded demonstration data or the browser disruption engine and report their offline state.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
