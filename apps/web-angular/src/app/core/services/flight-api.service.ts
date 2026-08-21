import { Injectable, signal } from '@angular/core';
import { delay, Observable, of } from 'rxjs';
import { Flight } from '../models/flight.model';
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

@Injectable({ providedIn: 'root' })
export class FlightApiService {
  readonly state = signal<Flight[]>(SEEDED_FLIGHTS.map((flight) => ({ ...flight })));
  getFlights(): Observable<Flight[]> {
    return of(this.state()).pipe(delay(250));
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
}
