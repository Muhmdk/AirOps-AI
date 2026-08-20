import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/placeholders/login.page').then(m => m.LoginPage) },
  { path: 'overview', canActivate: [authGuard], loadComponent: () => import('./features/overview/overview.page').then(m => m.OverviewPage), title: 'Network Overview · AirOps' },
  { path: 'flights', canActivate: [authGuard], loadComponent: () => import('./features/flights/flights.page').then(m => m.FlightsPage), title: 'Flights · AirOps' },
  { path: 'flights/:id', canActivate: [authGuard], loadComponent: () => import('./features/flights/flight-detail.page').then(m => m.FlightDetailPage), title: 'Flight Details · AirOps' },
  { path: 'disruptions', canActivate: [authGuard], loadComponent: () => import('./features/disruptions/disruptions.page').then(m => m.DisruptionsPage), title: 'Disruptions · AirOps' },
  { path: 'disruptions/scenarios', canActivate: [authGuard], loadComponent: () => import('./features/disruptions/scenario-lab.page').then(m => m.ScenarioLabPage), title: 'Scenario Lab · AirOps' },
  { path: 'disruptions/:id', canActivate: [authGuard], loadComponent: () => import('./features/disruptions/disruption-detail.page').then(m => m.DisruptionDetailPage), title: 'Disruption Impact · AirOps' },
  { path: 'recovery-plans', canActivate: [authGuard], loadComponent: () => import('./features/recovery/recovery-plans.page').then(m => m.RecoveryPlansPage), title: 'Recovery Plans · AirOps' },
  { path: 'recovery-plans/:disruptionId', canActivate: [authGuard], loadComponent: () => import('./features/recovery/recovery-comparison.page').then(m => m.RecoveryComparisonPage), title: 'Recovery Comparison · AirOps' },
  { path: 'airports', canActivate: [authGuard], loadComponent: () => import('./features/airports/airports.page').then(m => m.AirportsPage), title: 'Airports · AirOps' },
  { path: 'airports/:code', canActivate: [authGuard], loadComponent: () => import('./features/airports/airport-detail.page').then(m => m.AirportDetailPage), title: 'Airport Operations · AirOps' },
  { path: 'aircraft', canActivate: [authGuard], loadComponent: () => import('./features/aircraft/aircraft.page').then(m => m.AircraftPage), title: 'Aircraft · AirOps' },
  { path: 'aircraft/:registration', canActivate: [authGuard], loadComponent: () => import('./features/aircraft/aircraft-detail.page').then(m => m.AircraftDetailPage), title: 'Aircraft Details · AirOps' },
  { path: 'event-timeline', canActivate: [authGuard], loadComponent: () => import('./features/events/event-timeline.page').then(m => m.EventTimelinePage), title: 'Operational Events · AirOps' },
  { path: ':workspace', canActivate: [authGuard], loadComponent: () => import('./features/placeholders/workspace.page').then(m => m.WorkspacePage), data: { title: 'Operations workspace', description: 'This operational workspace is connected to the Phase 2 application shell.' } },
  { path: '', pathMatch: 'full', redirectTo: 'overview' },
  { path: '**', redirectTo: 'overview' }
];
