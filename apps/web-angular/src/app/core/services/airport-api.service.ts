import { Injectable, signal } from '@angular/core';
import { delay, Observable, of } from 'rxjs';
import { AirportOperation } from '../models/airport.model';
import { Disruption } from '../models/disruption.model';
import { RecoveryPlan } from '../models/recovery.model';

export const SEEDED_AIRPORTS: AirportOperation[] = [
  {
    code: 'YYZ',
    name: 'Toronto Pearson International',
    city: 'Toronto',
    province: 'ON',
    timezone: 'EDT',
    risk: 'High',
    health: 62,
    averageDelay: 38,
    departures: 184,
    arrivals: 177,
    atRisk: 12,
    gatesUsed: 58,
    gatesTotal: 65,
    weather: 'Thunderstorms',
    temperature: 24,
    wind: 'SW 28 km/h',
    visibility: '5 km',
  },
  {
    code: 'YUL',
    name: 'Montréal–Trudeau International',
    city: 'Montréal',
    province: 'QC',
    timezone: 'EDT',
    risk: 'Moderate',
    health: 78,
    averageDelay: 21,
    departures: 116,
    arrivals: 109,
    atRisk: 5,
    gatesUsed: 42,
    gatesTotal: 52,
    weather: 'Light rain',
    temperature: 21,
    wind: 'W 14 km/h',
    visibility: '12 km',
  },
  {
    code: 'YYC',
    name: 'Calgary International',
    city: 'Calgary',
    province: 'AB',
    timezone: 'MDT',
    risk: 'Low',
    health: 91,
    averageDelay: 8,
    departures: 92,
    arrivals: 88,
    atRisk: 1,
    gatesUsed: 31,
    gatesTotal: 42,
    weather: 'Clear',
    temperature: 18,
    wind: 'NW 9 km/h',
    visibility: '24 km',
  },
  {
    code: 'YVR',
    name: 'Vancouver International',
    city: 'Vancouver',
    province: 'BC',
    timezone: 'PDT',
    risk: 'Low',
    health: 94,
    averageDelay: 6,
    departures: 128,
    arrivals: 121,
    atRisk: 2,
    gatesUsed: 39,
    gatesTotal: 50,
    weather: 'Partly cloudy',
    temperature: 17,
    wind: 'W 11 km/h',
    visibility: '20 km',
  },
  {
    code: 'YWG',
    name: 'Winnipeg Richardson International',
    city: 'Winnipeg',
    province: 'MB',
    timezone: 'CDT',
    risk: 'Moderate',
    health: 81,
    averageDelay: 17,
    departures: 51,
    arrivals: 48,
    atRisk: 3,
    gatesUsed: 15,
    gatesTotal: 22,
    weather: 'Overcast',
    temperature: 20,
    wind: 'S 19 km/h',
    visibility: '14 km',
  },
  {
    code: 'YHZ',
    name: 'Halifax Stanfield International',
    city: 'Halifax',
    province: 'NS',
    timezone: 'ADT',
    risk: 'Low',
    health: 89,
    averageDelay: 9,
    departures: 47,
    arrivals: 44,
    atRisk: 1,
    gatesUsed: 18,
    gatesTotal: 28,
    weather: 'Clear',
    temperature: 19,
    wind: 'SE 8 km/h',
    visibility: '25 km',
  },
];

@Injectable({ providedIn: 'root' })
export class AirportApiService {
  readonly state = signal<AirportOperation[]>(SEEDED_AIRPORTS.map((airport) => ({ ...airport })));
  getAirports(): Observable<AirportOperation[]> {
    return of(this.state()).pipe(delay(220));
  }
  reset() {
    this.state.set(SEEDED_AIRPORTS.map((airport) => ({ ...airport })));
  }
  apply(disruption: Disruption) {
    this.state.update((airports) =>
      airports.map((airport) =>
        airport.code !== disruption.airport
          ? airport
          : {
              ...airport,
              risk: disruption.severity === 'Moderate' ? 'Moderate' : 'High',
              health: Math.max(30, airport.health - Math.round(disruption.durationMinutes / 18)),
              averageDelay: Math.max(
                airport.averageDelay,
                Math.round(disruption.impact.recoveryMinutes * 0.42),
              ),
              atRisk: airport.atRisk + disruption.impact.affectedFlights,
              gatesUsed: Math.min(
                airport.gatesTotal,
                airport.gatesUsed + disruption.impact.gateConflicts,
              ),
              weather:
                disruption.type === 'Severe weather' ? 'Severe thunderstorms' : airport.weather,
            },
      ),
    );
  }
  applyRecovery(airportCode: string, plan: RecoveryPlan) {
    this.state.update((airports) =>
      airports.map((airport) =>
        airport.code !== airportCode
          ? airport
          : {
              ...airport,
              health: Math.min(100, airport.health + 12),
              averageDelay: Math.max(0, Math.min(airport.averageDelay, plan.expectedDelayMinutes)),
              atRisk: Math.max(0, airport.atRisk - plan.flightsAffected.length),
              gatesUsed: Math.max(0, airport.gatesUsed - 1),
            },
      ),
    );
  }
}
