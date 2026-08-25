import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { catchError, delay, map, Observable, of, tap, throwError } from 'rxjs';
import { AircraftOperation, AircraftStatus } from '../models/aircraft.model';
import { Disruption } from '../models/disruption.model';
import { RecoveryPlan } from '../models/recovery.model';

export const SEEDED_AIRCRAFT: AircraftOperation[] = [
  {
    registration: 'C-FVLX',
    type: 'Boeing 787-9',
    family: 'Widebody',
    status: 'In service',
    location: 'YYZ',
    nextFlight: 'AC103',
    nextDeparture: '09:15',
    utilization: 87,
    cycles: 2,
    hours: 13.4,
    maintenanceDue: 146,
    health: 94,
    seats: 298,
    range: '14,140 km',
  },
  {
    registration: 'C-GROV',
    type: 'Airbus A220-300',
    family: 'Narrowbody',
    status: 'Turnaround',
    location: 'YYZ',
    nextFlight: 'AC418',
    nextDeparture: '09:40',
    utilization: 76,
    cycles: 4,
    hours: 8.1,
    maintenanceDue: 32,
    health: 78,
    seats: 137,
    range: '6,300 km',
  },
  {
    registration: 'C-GFAF',
    type: 'Airbus A330-300',
    family: 'Widebody',
    status: 'In service',
    location: 'YUL',
    nextFlight: 'AC791',
    nextDeparture: '10:05',
    utilization: 91,
    cycles: 2,
    hours: 14.8,
    maintenanceDue: 84,
    health: 88,
    seats: 297,
    range: '11,750 km',
  },
  {
    registration: 'C-FSIP',
    type: 'Boeing 737 MAX 8',
    family: 'Narrowbody',
    status: 'Turnaround',
    location: 'YYC',
    nextFlight: 'AC156',
    nextDeparture: '10:20',
    utilization: 82,
    cycles: 5,
    hours: 9.7,
    maintenanceDue: 61,
    health: 86,
    seats: 169,
    range: '6,570 km',
  },
  {
    registration: 'C-FITL',
    type: 'Boeing 777-300ER',
    family: 'Widebody',
    status: 'Available',
    location: 'YYZ',
    nextFlight: 'AC882',
    nextDeparture: '20:45',
    utilization: 48,
    cycles: 1,
    hours: 7.3,
    maintenanceDue: 212,
    health: 97,
    seats: 400,
    range: '13,650 km',
  },
  {
    registration: 'C-GJYE',
    type: 'Airbus A320-200',
    family: 'Narrowbody',
    status: 'Unavailable',
    location: 'YUL',
    nextFlight: 'Unassigned',
    nextDeparture: '—',
    utilization: 0,
    cycles: 0,
    hours: 0,
    maintenanceDue: 0,
    health: 42,
    seats: 146,
    range: '6,150 km',
  },
];

interface AircraftApiResponse {
  registration: string;
  type: string;
  family: string;
  status: AircraftStatus;
  location: string;
  nextFlight: string;
  nextDeparture: string;
  utilization: number;
  cycles: number;
  hours: number;
  maintenanceDue: number;
  health: number;
  seats: number;
  range: string;
}

@Injectable({ providedIn: 'root' })
export class AircraftApiService {
  readonly state = signal<AircraftOperation[]>(
    SEEDED_AIRCRAFT.map((aircraft) => ({ ...aircraft })),
  );
  readonly source = signal<'loading' | 'backend' | 'fallback'>('fallback');
  readonly connectionError = signal<string | null>(null);

  constructor(private readonly http?: HttpClient) {}

  getAircraft(): Observable<AircraftOperation[]> {
    if (!this.http) return of(this.state()).pipe(delay(220));

    this.source.set('loading');
    this.connectionError.set(null);
    return this.http.get<AircraftApiResponse[]>('/api/aircraft').pipe(
      map(responses => responses.map(response => ({ ...response }))),
      tap(fleet => {
        this.state.set(fleet);
        this.source.set('backend');
      }),
      catchError(() => {
        this.source.set('fallback');
        this.connectionError.set('Backend unavailable; showing demonstration aircraft data.');
        return of(this.state());
      })
    );
  }

  getAircraftByRegistration(registration: string): Observable<AircraftOperation> {
    const fallback = this.state().find(aircraft =>
      aircraft.registration.toUpperCase() === registration.toUpperCase());
    if (!this.http) {
      return fallback
        ? of(fallback).pipe(delay(150))
        : throwError(() => new Error(`Aircraft '${registration}' was not found.`));
    }

    return this.http.get<AircraftApiResponse>(
      `/api/aircraft/${encodeURIComponent(registration)}`
    ).pipe(
      map(response => ({ ...response })),
      tap(aircraft => {
        this.state.update(fleet => [
          aircraft,
          ...fleet.filter(item => item.registration !== aircraft.registration),
        ]);
        this.source.set('backend');
        this.connectionError.set(null);
      }),
      catchError(error => {
        if (!fallback || (error instanceof HttpErrorResponse && error.status === 404))
          return throwError(() => error);
        this.source.set('fallback');
        this.connectionError.set('Backend unavailable; showing demonstration aircraft data.');
        return of(fallback);
      })
    );
  }
  reset() {
    this.state.set(SEEDED_AIRCRAFT.map((aircraft) => ({ ...aircraft })));
  }
  apply(disruption: Disruption) {
    this.state.update((fleet) =>
      fleet.map((aircraft) =>
        aircraft.nextFlight !== disruption.primaryFlight
          ? aircraft
          : {
              ...aircraft,
              status: disruption.type === 'Aircraft maintenance' ? 'Unavailable' : 'Turnaround',
              health: Math.max(
                35,
                aircraft.health - (disruption.severity === 'Critical' ? 28 : 16),
              ),
              utilization: Math.max(
                0,
                aircraft.utilization - Math.round(disruption.durationMinutes / 10),
              ),
              maintenanceDue:
                disruption.type === 'Aircraft maintenance' ? 0 : aircraft.maintenanceDue,
            },
      ),
    );
  }
  applyRecovery(plan: RecoveryPlan) {
    if (plan.action !== 'Swap aircraft' || !plan.aircraftAffected[0]) return;
    const replacement = plan.aircraftAffected[0];
    const flightId = plan.flightsAffected[0];
    this.state.update((fleet) =>
      fleet.map((aircraft) =>
        aircraft.registration === replacement
          ? {
              ...aircraft,
              status: 'In service',
              nextFlight: flightId,
              utilization: Math.min(100, aircraft.utilization + 12),
            }
          : aircraft.nextFlight === flightId
            ? { ...aircraft, status: 'Available', nextFlight: 'Unassigned', nextDeparture: '—' }
            : aircraft,
      ),
    );
  }
}
