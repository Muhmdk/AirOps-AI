import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { catchError, delay, Observable, of, tap, throwError } from 'rxjs';
import { PassengerJourney } from '../models/passenger.model';

export const SEEDED_PASSENGER_JOURNEYS: PassengerJourney[] = [
  {
    id: 'PAX-001', bookingReference: '7Q4K2M', leadPassenger: 'Aisha Rahman', partySize: 3,
    loyaltyTier: 'Aeroplan 50K', currentFlightId: 'AC103', connectingFlightId: 'AC205',
    originCode: 'YYZ', connectionAirport: 'YVR', destinationCode: 'YYC',
    minimumConnectionMinutes: 45, availableConnectionMinutes: 16, connectionShortfallMinutes: 29,
    status: 'At risk', riskScore: 91,
    specialServices: ['Wheelchair assistance', 'Checked bags through to YYC'],
    alternativeFlights: ['AC125 · YYZ → YYC · 12:10', 'AC211 · YVR → YYC · 14:25'],
    selectedAlternativeFlight: null, estimatedCareCost: 780, rebookingNotes: null,
    updatedAt: '2026-08-06T13:12:00Z',
  },
  {
    id: 'PAX-002', bookingReference: 'M8T3LX', leadPassenger: 'Daniel Tremblay', partySize: 2,
    loyaltyTier: 'Aeroplan 35K', currentFlightId: 'AC418', connectingFlightId: 'AC834',
    originCode: 'YYZ', connectionAirport: 'YUL', destinationCode: 'YHZ',
    minimumConnectionMinutes: 45, availableConnectionMinutes: 22, connectionShortfallMinutes: 23,
    status: 'At risk', riskScore: 84,
    specialServices: ['French-language service', 'Checked bags through to YHZ'],
    alternativeFlights: ['AC612 · YYZ → YHZ · 12:35', 'AC836 · YUL → YHZ · 14:10'],
    selectedAlternativeFlight: null, estimatedCareCost: 520, rebookingNotes: null,
    updatedAt: '2026-08-06T13:12:00Z',
  },
  {
    id: 'PAX-003', bookingReference: 'C2N9PW', leadPassenger: 'Sofia Chen', partySize: 1,
    loyaltyTier: 'Aeroplan 75K', currentFlightId: 'AC791', connectingFlightId: 'UA188',
    originCode: 'YUL', connectionAirport: 'LAX', destinationCode: 'SFO',
    minimumConnectionMinutes: 60, availableConnectionMinutes: 0, connectionShortfallMinutes: 60,
    status: 'Misconnected', riskScore: 97,
    specialServices: ['Priority protection', 'Checked bag retrieval required'],
    alternativeFlights: ['UA522 · LAX → SFO · 15:20', 'AC745 · YUL → SFO · 17:05'],
    selectedAlternativeFlight: null, estimatedCareCost: 940, rebookingNotes: null,
    updatedAt: '2026-08-06T13:12:00Z',
  },
  {
    id: 'PAX-004', bookingReference: 'R5J1VD', leadPassenger: 'Marcus Johnson', partySize: 4,
    loyaltyTier: 'Aeroplan Member', currentFlightId: 'AC156', connectingFlightId: 'AC882',
    originCode: 'YYC', connectionAirport: 'YYZ', destinationCode: 'CDG',
    minimumConnectionMinutes: 60, availableConnectionMinutes: 72, connectionShortfallMinutes: 0,
    status: 'Protected', riskScore: 28,
    specialServices: ['Family seating', 'Checked bags through to CDG'],
    alternativeFlights: ['AC872 · YYZ → CDG · 22:10'],
    selectedAlternativeFlight: null, estimatedCareCost: 0, rebookingNotes: null,
    updatedAt: '2026-08-06T13:12:00Z',
  },
  {
    id: 'PAX-005', bookingReference: 'F6B7QA', leadPassenger: 'Elena Rossi', partySize: 2,
    loyaltyTier: 'Aeroplan 25K', currentFlightId: 'AC882', connectingFlightId: 'AF144',
    originCode: 'YYZ', connectionAirport: 'CDG', destinationCode: 'FCO',
    minimumConnectionMinutes: 75, availableConnectionMinutes: 41, connectionShortfallMinutes: 34,
    status: 'At risk', riskScore: 76,
    specialServices: ['Vegetarian meals', 'Checked bags through to FCO'],
    alternativeFlights: ['AF1604 · CDG → FCO · 09:40', 'AZ319 · CDG → FCO · 11:15'],
    selectedAlternativeFlight: null, estimatedCareCost: 610, rebookingNotes: null,
    updatedAt: '2026-08-06T13:12:00Z',
  },
  {
    id: 'PAX-006', bookingReference: 'K4H8ZS', leadPassenger: 'Noah Williams', partySize: 1,
    loyaltyTier: 'Aeroplan Super Elite', currentFlightId: 'AC103', connectingFlightId: 'AC205',
    originCode: 'YYZ', connectionAirport: 'YVR', destinationCode: 'YYC',
    minimumConnectionMinutes: 45, availableConnectionMinutes: 18, connectionShortfallMinutes: 27,
    status: 'Rebooked', riskScore: 18, specialServices: ['Priority protection'],
    alternativeFlights: ['AC125 · YYZ → YYC · 12:10'],
    selectedAlternativeFlight: 'AC125 · YYZ → YYC · 12:10', estimatedCareCost: 240,
    rebookingNotes: 'Protected on the direct service before the connection window closed.',
    updatedAt: '2026-08-06T13:12:00Z',
  },
];

@Injectable({ providedIn: 'root' })
export class PassengerApiService {
  readonly state = signal<PassengerJourney[]>(
    SEEDED_PASSENGER_JOURNEYS.map(item => ({ ...item })),
  );
  readonly source = signal<'loading' | 'backend' | 'fallback'>('fallback');
  readonly connectionError = signal<string | null>(null);

  constructor(private readonly http?: HttpClient) {}

  getPassengers(): Observable<PassengerJourney[]> {
    if (!this.http) return of(this.state()).pipe(delay(180));
    this.source.set('loading');
    this.connectionError.set(null);
    return this.http.get<PassengerJourney[]>('/api/passengers').pipe(
      tap(items => {
        this.state.set(items);
        this.source.set('backend');
      }),
      catchError(() => this.offline(this.state())),
    );
  }

  getPassenger(id: string): Observable<PassengerJourney> {
    const fallback = this.state().find(item => item.id.toUpperCase() === id.toUpperCase());
    if (!this.http) return fallback
      ? of(fallback).pipe(delay(120))
      : throwError(() => new Error(`Passenger journey '${id}' was not found.`));
    return this.http.get<PassengerJourney>(`/api/passengers/${encodeURIComponent(id)}`).pipe(
      tap(item => this.hydrate(item)),
      catchError(error => fallback && this.isOffline(error)
        ? this.offline(fallback)
        : throwError(() => error)),
    );
  }

  rebook(id: string, alternativeFlight: string, notes: string): Observable<PassengerJourney> {
    if (!this.http) return this.rebookOffline(id, alternativeFlight, notes);
    return this.http.post<PassengerJourney>(
      `/api/passengers/${encodeURIComponent(id)}/rebook`,
      { alternativeFlight, notes },
    ).pipe(
      tap(item => this.hydrate(item)),
      catchError(error => this.isOffline(error)
        ? this.rebookOffline(id, alternativeFlight, notes)
        : throwError(() => error)),
    );
  }

  private hydrate(item: PassengerJourney) {
    this.state.update(items => [item, ...items.filter(current => current.id !== item.id)]);
    this.source.set('backend');
    this.connectionError.set(null);
  }

  private rebookOffline(id: string, alternativeFlight: string, notes: string) {
    const item = this.state().find(current => current.id === id);
    if (!item) return throwError(() => new Error('Passenger journey not found.'));
    if (item.status === 'Rebooked')
      return throwError(() => new Error('This passenger journey has already been rebooked.'));
    const updated: PassengerJourney = {
      ...item,
      status: 'Rebooked',
      riskScore: Math.min(item.riskScore, 18),
      selectedAlternativeFlight: alternativeFlight,
      rebookingNotes: notes,
      updatedAt: new Date().toISOString(),
    };
    this.state.update(items => [updated, ...items.filter(current => current.id !== id)]);
    return this.offline(updated);
  }

  private isOffline(error: unknown) {
    return !(error instanceof HttpErrorResponse) || error.status === 0 || error.status >= 500;
  }

  private offline<T>(value: T): Observable<T> {
    this.source.set('fallback');
    this.connectionError.set('Backend unavailable; using demonstration passenger journeys.');
    return of(value);
  }
}
