import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { catchError, delay, map, Observable, of, tap, throwError } from 'rxjs';
import { Flight, FlightStatus } from '../models/flight.model';
import { Disruption } from '../models/disruption.model';
import { RecoveryPlan } from '../models/recovery.model';

export const SEEDED_FLIGHTS: Flight[] = [
  {
    id: 'AC103',
    route: 'YYZ → YVR',
    origin: 'Toronto',
    destination: 'Vancouver',
    departure: '09:15',
    arrival: '11:28',
    aircraft: 'Boeing 787-9 · C-FVLX',
    gate: 'D24',
    status: 'At risk',
    risk: 82,
    passengers: 286,
    connections: 47,
    delay: 68,
    riskLabel: 'Severe weather',
  },
  {
    id: 'AC418',
    route: 'YYZ → YUL',
    origin: 'Toronto',
    destination: 'Montréal',
    departure: '09:40',
    arrival: '10:58',
    aircraft: 'Airbus A220-300 · C-GROV',
    gate: 'D31',
    status: 'Delayed',
    risk: 71,
    passengers: 124,
    connections: 31,
    delay: 42,
    riskLabel: 'Late inbound aircraft',
  },
  {
    id: 'AC791',
    route: 'YUL → LAX',
    origin: 'Montréal',
    destination: 'Los Angeles',
    departure: '10:05',
    arrival: '12:49',
    aircraft: 'Airbus A330-300 · C-GFAF',
    gate: 'A52',
    status: 'At risk',
    risk: 67,
    passengers: 241,
    connections: 38,
    delay: 36,
    riskLabel: 'Short turnaround',
  },
  {
    id: 'AC156',
    route: 'YYC → YYZ',
    origin: 'Calgary',
    destination: 'Toronto',
    departure: '10:20',
    arrival: '16:04',
    aircraft: 'Boeing 737 MAX 8 · C-FSIP',
    gate: 'C18',
    status: 'Boarding',
    risk: 43,
    passengers: 171,
    connections: 22,
    delay: 12,
    riskLabel: 'Airport congestion',
  },
  {
    id: 'AC882',
    route: 'YYZ → CDG',
    origin: 'Toronto',
    destination: 'Paris',
    departure: '20:45',
    arrival: '10:05',
    aircraft: 'Boeing 777-300ER · C-FITL',
    gate: 'E73',
    status: 'On time',
    risk: 18,
    passengers: 356,
    connections: 64,
    delay: 0,
    riskLabel: 'Normal operations',
  },
];

interface FlightApiResponse {
  id: string;
  route: string;
  originCode: string;
  origin: string;
  destinationCode: string;
  destination: string;
  scheduledDeparture: string;
  estimatedDeparture: string;
  scheduledArrival: string;
  estimatedArrival: string;
  aircraftRegistration: string;
  aircraftType: string;
  gate: string;
  status: 'OnTime' | 'Delayed' | 'Boarding' | 'AtRisk' | 'Cancelled';
  risk: number;
  delayMinutes: number;
  passengers: number;
  connectingPassengers: number;
  riskLabel: string;
}

export type FlightDataSource = 'loading' | 'backend' | 'fallback';

@Injectable({ providedIn: 'root' })
export class FlightApiService {
  readonly state = signal<Flight[]>(SEEDED_FLIGHTS.map((flight) => ({ ...flight })));
  readonly source = signal<FlightDataSource>('fallback');
  readonly connectionError = signal<string | null>(null);

  constructor(private readonly http?: HttpClient) {}

  getFlights(): Observable<Flight[]> {
    if (!this.http) return of(this.state()).pipe(delay(250));

    this.source.set('loading');
    this.connectionError.set(null);
    return this.http.get<FlightApiResponse[]>('/api/flights').pipe(
      map(responses => responses.map(response => this.toFlight(response))),
      tap(flights => {
        this.state.set(flights);
        this.source.set('backend');
      }),
      catchError(() => {
        this.source.set('fallback');
        this.connectionError.set('Backend unavailable; showing demonstration flight data.');
        return of(this.state());
      })
    );
  }

  getFlight(id: string): Observable<Flight> {
    const fallback = this.state().find(flight => flight.id.toUpperCase() === id.toUpperCase());
    if (!this.http) {
      return fallback
        ? of(fallback).pipe(delay(150))
        : throwError(() => new Error(`Flight '${id}' was not found.`));
    }

    return this.http.get<FlightApiResponse>(`/api/flights/${encodeURIComponent(id)}`).pipe(
      map(response => this.toFlight(response)),
      tap(flight => {
        this.state.update(flights => [flight, ...flights.filter(item => item.id !== flight.id)]);
        this.source.set('backend');
        this.connectionError.set(null);
      }),
      catchError(error => {
        if (!fallback || (error instanceof HttpErrorResponse && error.status === 404))
          return throwError(() => error);
        this.source.set('fallback');
        this.connectionError.set('Backend unavailable; showing demonstration flight data.');
        return of(fallback);
      })
    );
  }
  reset() {
    this.state.set(SEEDED_FLIGHTS.map((flight) => ({ ...flight })));
  }
  apply(disruption: Disruption) {
    const impacts = new Map(disruption.impact.flights.map((flight) => [flight.id, flight]));
    this.state.update((flights) =>
      flights.map((flight) => {
        const impact = impacts.get(flight.id);
        if (!impact) return flight;
        const baseline = SEEDED_FLIGHTS.find((item) => item.id === flight.id)!;
        const overlapping = flight.riskLabel !== baseline.riskLabel;
        const baseDelay = Math.max(flight.delay, impact.propagatedDelay);
        const baseRisk = Math.max(flight.risk, 55 + Math.round(impact.propagatedDelay / 2));
        return {
          ...flight,
          delay: overlapping ? baseDelay + Math.round(impact.propagatedDelay * 0.2) : baseDelay,
          risk: Math.min(99, baseRisk + (overlapping ? 8 : 0)),
          status: impact.propagatedDelay >= 30 ? 'At risk' : 'Delayed',
          riskLabel: overlapping ? `${flight.riskLabel} + ${disruption.type}` : disruption.type,
        };
      }),
    );
  }
  applyRecovery(plan: RecoveryPlan) {
    const affected = new Set(plan.flightsAffected);
    this.state.update((flights) =>
      flights.map((flight) =>
        !affected.has(flight.id)
          ? flight
          : {
              ...flight,
              delay: plan.expectedDelayMinutes,
              risk:
                plan.operationalRisk === 'Low' ? 24 : plan.operationalRisk === 'Medium' ? 42 : 58,
              status:
                plan.expectedDelayMinutes <= 15
                  ? 'On time'
                  : plan.expectedDelayMinutes < 35
                    ? 'Delayed'
                    : 'At risk',
              riskLabel: `Recovery: ${plan.action}`,
              gate: plan.action === 'Change gate' ? this.nextGate(flight.gate) : flight.gate,
            },
      ),
    );
  }
  private nextGate(gate: string) {
    const match = gate.match(/^(\D+)(\d+)$/);
    return match ? `${match[1]}${Number(match[2]) + 2}` : gate;
  }

  private toFlight(response: FlightApiResponse): Flight {
    const statuses: Record<FlightApiResponse['status'], FlightStatus> = {
      OnTime: 'On time',
      Delayed: 'Delayed',
      Boarding: 'Boarding',
      AtRisk: 'At risk',
      Cancelled: 'Cancelled',
    };
    return {
      id: response.id,
      route: response.route,
      origin: response.origin,
      destination: response.destination,
      departure: this.toClockTime(response.scheduledDeparture),
      arrival: this.toClockTime(response.estimatedArrival),
      aircraft: `${response.aircraftType} · ${response.aircraftRegistration}`,
      gate: response.gate,
      status: statuses[response.status],
      risk: response.risk,
      passengers: response.passengers,
      connections: response.connectingPassengers,
      delay: response.delayMinutes,
      riskLabel: response.riskLabel,
    };
  }

  private toClockTime(timestamp: string) {
    return timestamp.match(/T(\d{2}:\d{2})/)?.[1] ?? timestamp;
  }
}
