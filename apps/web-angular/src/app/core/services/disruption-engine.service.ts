import { Injectable, computed, signal } from '@angular/core';
import {
  CrewDutyImpact,
  Disruption,
  DisruptionAuditEntry,
  DisruptionScenario,
  GateConflictImpact,
  ImpactedFlight,
  NetworkImpact,
  PassengerConnectionImpact,
  NetworkMutation,
  NetworkSnapshot,
  ScenarioRun,
  CompoundDisruptionImpact,
} from '../models/disruption.model';
import { SEEDED_FLIGHTS } from './flight-api.service';
import { OperationsEventService } from './operations-event.service';
import { FlightApiService } from './flight-api.service';
import { AirportApiService } from './airport-api.service';
import { AircraftApiService } from './aircraft-api.service';

const ROTATIONS: Record<string, string[]> = {
  AC103: ['AC103', 'AC205', 'AC221'],
  AC418: ['AC418', 'AC522'],
  AC791: ['AC791', 'AC834'],
};
const TYPE_DELAY: Record<string, number> = {
  'Severe weather': 70,
  'Aircraft maintenance': 95,
  'Late incoming aircraft': 45,
  'Gate conflict': 25,
  'Airport congestion': 35,
  'Crew timing issue': 60,
  'Runway closure': 110,
  'Air traffic restriction': 50,
};

@Injectable({ providedIn: 'root' })
export class DisruptionEngineService {
  private readonly sequence = signal(3);
  readonly disruptions = signal<Disruption[]>([]);
  readonly auditEntries = signal<DisruptionAuditEntry[]>([]);
  readonly scenarioRuns = signal<ScenarioRun[]>([]);
  readonly active = computed(() => this.disruptions().filter((d) => d.status !== 'Resolved'));
  readonly compoundImpacts = computed<CompoundDisruptionImpact[]>(() => {
    const active = this.active();
    const overlaps: CompoundDisruptionImpact[] = [];
    for (let left = 0; left < active.length; left++)
      for (let right = left + 1; right < active.length; right++) {
        const a = active[left];
        const b = active[right];
        const aFlights = new Set(a.impact.flights.map((flight) => flight.id));
        const sharedFlights = b.impact.flights
          .filter((flight) => aFlights.has(flight.id))
          .map((flight) => flight.id);
        const shared = [...(a.airport === b.airport ? [a.airport] : []), ...sharedFlights];
        if (!shared.length) continue;
        overlaps.push({
          id: `CMP-${a.id}-${b.id}`,
          disruptionIds: [a.id, b.id],
          reason: sharedFlights.length
            ? 'Overlapping aircraft rotation'
            : 'Shared airport capacity constraint',
          sharedEntities: shared,
          additionalDelayMinutes: Math.round(
            (a.impact.recoveryMinutes + b.impact.recoveryMinutes) * 0.16,
          ),
          additionalPassengers: Math.round(
            Math.min(a.impact.affectedPassengers, b.impact.affectedPassengers) * 0.18,
          ),
          additionalCost: Math.round(
            (a.impact.estimatedOperationalCost + b.impact.estimatedOperationalCost) * 0.12,
          ),
          severity: a.severity === 'Critical' || b.severity === 'Critical' ? 'Critical' : 'High',
        });
      }
    return overlaps;
  });
  readonly totalImpact = computed(() => {
    const base = this.active().reduce(
      (sum, d) => ({
        passengers: sum.passengers + d.impact.affectedPassengers,
        cost: sum.cost + d.impact.estimatedOperationalCost,
        connections: sum.connections + d.impact.missedConnections,
      }),
      { passengers: 0, cost: 0, connections: 0 },
    );
    return this.compoundImpacts().reduce(
      (sum, overlap) => ({
        ...sum,
        passengers: sum.passengers + overlap.additionalPassengers,
        cost: sum.cost + overlap.additionalCost,
      }),
      base,
    );
  });

  constructor(
    private readonly events?: OperationsEventService,
    private readonly flightApi?: FlightApiService,
    private readonly airportApi?: AirportApiService,
    private readonly aircraftApi?: AircraftApiService,
  ) {
    const restored = this.persistenceEnabled()
      ? this.read<Disruption[]>('airops-disruptions')
      : null;
    this.disruptions.set(restored !== null ? restored : this.seedDisruptions());
    this.auditEntries.set(
      this.persistenceEnabled()
        ? (this.read<DisruptionAuditEntry[]>('airops-disruption-audit') ?? [])
        : [],
    );
    this.scenarioRuns.set(
      this.persistenceEnabled() ? (this.read<ScenarioRun[]>('airops-scenario-runs') ?? []) : [],
    );
    const highest = this.disruptions().reduce(
      (max, item) => Math.max(max, Number(item.id.split('-')[1]) || 0),
      2,
    );
    this.sequence.set(highest + 1);
    this.recomputeNetwork();
  }

  private seedDisruptions() {
    return [
      this.build(
        {
          type: 'Severe weather',
          severity: 'Critical',
          airport: 'YYZ',
          flightId: 'AC103',
          durationMinutes: 120,
        },
        'DSP-001',
        '09:08',
      ),
      this.build(
        {
          type: 'Late incoming aircraft',
          severity: 'High',
          airport: 'YYZ',
          flightId: 'AC418',
          durationMinutes: 75,
        },
        'DSP-002',
        '09:04',
      ),
    ];
  }

  create(scenario: DisruptionScenario) {
    const before = this.snapshot();
    const id = `DSP-${String(this.sequence()).padStart(3, '0')}`;
    this.sequence.update((n) => n + 1);
    const disruption = this.build(
      scenario,
      id,
      new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
    );
    this.disruptions.update((items) => [disruption, ...items]);
    this.recomputeNetwork();
    this.recordAudit(
      disruption.id,
      'Created',
      `Triggered ${disruption.severity.toLowerCase()} ${disruption.type}`,
      before,
    );
    this.persist();
    this.events?.publish({
      time: disruption.startedAt,
      type: 'risk',
      title: `${disruption.type} · ${disruption.primaryFlight}`,
      detail: `${disruption.impact.affectedFlights} flights and ${disruption.impact.affectedPassengers} passengers affected`,
      accent: 'red',
      severity: disruption.severity === 'Moderate' ? 'Warning' : 'Critical',
      entityType: 'flight',
      entityId: disruption.primaryFlight,
      category: disruption.type === 'Severe weather' ? 'Weather' : 'Flight',
    });
    return disruption;
  }

  resolve(id: string) {
    const disruption = this.get(id);
    const before = this.snapshot();
    this.disruptions.update((items) =>
      items.map((d) => (d.id === id ? { ...d, status: 'Resolved' as const } : d)),
    );
    this.recomputeNetwork();
    if (disruption)
      this.recordAudit(
        id,
        'Resolved',
        `Resolved ${disruption.type} affecting ${disruption.primaryFlight}`,
        before,
      );
    this.persist();
    if (disruption)
      this.events?.publish({
        time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
        type: 'ok',
        title: `Disruption resolved · ${id}`,
        detail: `${disruption.primaryFlight} returned to recovery monitoring`,
        accent: 'green',
        severity: 'Information',
        entityType: 'flight',
        entityId: disruption.primaryFlight,
        category: 'Flight',
      });
  }

  get(id: string) {
    return this.disruptions().find((d) => d.id === id);
  }

  runScenario(name: string, description: string, scenario: DisruptionScenario) {
    const before = this.captureNetworkSnapshot();
    const disruption = this.create(scenario);
    const run: ScenarioRun = {
      id: `RUN-${Date.now()}`,
      name,
      description,
      scenario,
      before,
      after: this.captureNetworkSnapshot(),
      disruptionId: disruption.id,
      runAt: new Date().toISOString(),
    };
    this.scenarioRuns.update((runs) => [run, ...runs]);
    this.persist();
    return run;
  }

  replay(run: ScenarioRun) {
    this.resetSimulation();
    return this.runScenario(`${run.name} · Replay`, run.description, run.scenario);
  }

  runNetworkStressTest() {
    this.resetSimulation();
    const scenarios: Array<[string, string, DisruptionScenario]> = [
      [
        'Stress · Toronto weather',
        'Critical weather constraint at the primary hub',
        {
          type: 'Severe weather',
          severity: 'Critical',
          airport: 'YYZ',
          flightId: 'AC103',
          durationMinutes: 120,
        },
      ],
      [
        'Stress · Pearson runway',
        'Concurrent runway capacity reduction at Pearson',
        {
          type: 'Runway closure',
          severity: 'High',
          airport: 'YYZ',
          flightId: 'AC418',
          durationMinutes: 75,
        },
      ],
      [
        'Stress · AC103 maintenance',
        'Mechanical constraint on the weather-affected rotation',
        {
          type: 'Aircraft maintenance',
          severity: 'Critical',
          airport: 'YYZ',
          flightId: 'AC103',
          durationMinutes: 90,
        },
      ],
    ];
    return scenarios.map(([name, description, scenario]) =>
      this.runScenario(name, description, scenario),
    );
  }

  resetSimulation() {
    this.disruptions.set([]);
    this.auditEntries.set([]);
    this.scenarioRuns.set([]);
    this.sequence.set(1);
    this.recomputeNetwork();
    this.persist();
    this.events?.publish({
      time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      type: 'ok',
      title: 'Simulation reset',
      detail: 'Network restored to its clean operational baseline',
      accent: 'green',
      severity: 'Information',
      category: 'Flight',
    });
  }

  private recomputeNetwork() {
    this.flightApi?.reset();
    this.airportApi?.reset();
    this.aircraftApi?.reset();
    for (const disruption of this.active()) {
      this.flightApi?.apply(disruption);
      this.airportApi?.apply(disruption);
      this.aircraftApi?.apply(disruption);
    }
  }

  private snapshot() {
    const values = new Map<string, string | number>();
    for (const flight of this.flightApi?.state() ?? []) {
      values.set(`Flight|${flight.id}|Status`, flight.status);
      values.set(`Flight|${flight.id}|Delay`, flight.delay);
      values.set(`Flight|${flight.id}|Risk`, flight.risk);
    }
    for (const airport of this.airportApi?.state() ?? []) {
      values.set(`Airport|${airport.code}|Health`, airport.health);
      values.set(`Airport|${airport.code}|Average delay`, airport.averageDelay);
      values.set(`Airport|${airport.code}|At-risk flights`, airport.atRisk);
    }
    for (const aircraft of this.aircraftApi?.state() ?? []) {
      values.set(`Aircraft|${aircraft.registration}|Status`, aircraft.status);
      values.set(`Aircraft|${aircraft.registration}|Health`, aircraft.health);
      values.set(`Aircraft|${aircraft.registration}|Utilization`, aircraft.utilization);
    }
    return values;
  }

  private recordAudit(
    disruptionId: string,
    action: 'Created' | 'Resolved',
    summary: string,
    before: Map<string, string | number>,
  ) {
    const after = this.snapshot();
    const changes: NetworkMutation[] = [];
    for (const [key, afterValue] of after) {
      const beforeValue = before.get(key);
      if (beforeValue !== undefined && beforeValue !== afterValue) {
        const [entityType, entityId, field] = key.split('|');
        changes.push({
          entityType: entityType as NetworkMutation['entityType'],
          entityId,
          field,
          before: beforeValue,
          after: afterValue,
        });
      }
    }
    this.auditEntries.update((entries) => [
      {
        id: `AUD-${Date.now()}`,
        disruptionId,
        action,
        actor: 'Maya Chen',
        timestamp: new Date().toISOString(),
        summary,
        changes,
      },
      ...entries,
    ]);
  }

  private persistenceEnabled() {
    return (
      typeof localStorage !== 'undefined' &&
      !!this.flightApi &&
      !!this.airportApi &&
      !!this.aircraftApi
    );
  }
  private persist() {
    if (!this.persistenceEnabled()) return;
    localStorage.setItem('airops-disruptions', JSON.stringify(this.disruptions()));
    localStorage.setItem('airops-disruption-audit', JSON.stringify(this.auditEntries()));
    localStorage.setItem('airops-scenario-runs', JSON.stringify(this.scenarioRuns()));
  }
  private read<T>(key: string): T | null {
    try {
      const value = localStorage.getItem(key);
      return value ? (JSON.parse(value) as T) : null;
    } catch {
      return null;
    }
  }

  private captureNetworkSnapshot(): NetworkSnapshot {
    const airports = this.airportApi?.state() ?? [];
    const flights = this.flightApi?.state() ?? [];
    const aircraft = this.aircraftApi?.state() ?? [];
    const affected = flights.filter(
      (flight) => flight.status === 'At risk' || flight.status === 'Delayed',
    );
    return {
      networkHealth: airports.length
        ? Math.round(airports.reduce((sum, airport) => sum + airport.health, 0) / airports.length)
        : 100,
      atRiskFlights: affected.length,
      affectedPassengers: affected.reduce((sum, flight) => sum + flight.passengers, 0),
      availableAircraft: aircraft.filter((item) => item.status !== 'Unavailable').length,
      estimatedCost: this.totalImpact().cost,
    };
  }

  private build(s: DisruptionScenario, id: string, time: string): Disruption {
    const flight = SEEDED_FLIGHTS.find((f) => f.id === s.flightId) ?? SEEDED_FLIGHTS[0];
    const severityFactor = s.severity === 'Critical' ? 1.3 : s.severity === 'High' ? 1 : 0.7;
    const primaryDelay = Math.round(TYPE_DELAY[s.type] * severityFactor);
    const rotation = ROTATIONS[flight.id] ?? [flight.id];
    const impacted: ImpactedFlight[] = rotation.map((flightId, index) => {
      const base = SEEDED_FLIGHTS.find((f) => f.id === flightId);
      const passengers = base?.passengers ?? Math.max(90, flight.passengers - index * 45);
      const propagatedDelay = Math.max(12, Math.round(primaryDelay * Math.pow(0.72, index)));
      return {
        id: flightId,
        route: base?.route ?? (index === 1 ? 'YVR → YYC' : 'YYC → YVR'),
        originalDelay: index === 0 ? flight.delay : 0,
        propagatedDelay,
        passengers,
        missedConnections: Math.round((base?.connections ?? 24) * (propagatedDelay / 100)),
        reason: index === 0 ? s.type : `Aircraft rotation from ${flight.id}`,
      };
    });
    return {
      id,
      type: s.type,
      severity: s.severity,
      status: 'Active',
      airport: s.airport,
      primaryFlight: flight.id,
      startedAt: time,
      durationMinutes: s.durationMinutes,
      description: `${s.type} affecting ${s.airport} operations and the ${flight.id} aircraft rotation.`,
      impact: this.calculateImpact(impacted, s),
      createdAt: new Date().toISOString(),
    };
  }

  private calculateImpact(flights: ImpactedFlight[], scenario: DisruptionScenario): NetworkImpact {
    const multiplier =
      scenario.severity === 'Critical' ? 1.25 : scenario.severity === 'High' ? 1 : 0.75;
    const connections: PassengerConnectionImpact[] = flights.flatMap((flight, index) => {
      const connectionCount = Math.max(1, flight.missedConnections);
      const available = Math.max(-15, 55 - flight.propagatedDelay);
      return [
        {
          inboundFlight: flight.id,
          outboundFlight: index === 0 ? 'AC205' : `AC${340 + index * 18}`,
          connectionAirport: flight.route.split(' → ')[1],
          passengers: connectionCount,
          minimumConnectionMinutes: 45,
          availableConnectionMinutes: available,
          status: available < 0 ? 'Missed' : available < 45 ? 'At risk' : 'Protected',
        },
      ];
    });
    const gateDetails: GateConflictImpact[] = flights
      .slice(1)
      .filter((f) => f.propagatedDelay >= 25)
      .map((f, index) => ({
        airport: f.route.split(' → ')[0],
        gate: `C${42 + index}`,
        incomingFlight: f.id,
        occupyingFlight: `AC${340 + index * 22}`,
        overlapMinutes: Math.max(8, f.propagatedDelay - 18),
        severity: f.propagatedDelay > 50 ? 'Critical' : 'Warning',
      }));
    const crewDetails: CrewDutyImpact[] = flights.map((f, index) => {
      const projected = 650 + f.propagatedDelay + index * 35;
      const limit = 780;
      const remaining = limit - projected;
      return {
        crewId: `CREW-${118 + index}`,
        flightId: f.id,
        role: index ? 'Cabin crew' : 'Flight deck',
        projectedDutyMinutes: projected,
        legalLimitMinutes: limit,
        remainingMinutes: remaining,
        status: remaining < 0 ? 'Exceeded' : remaining < 60 ? 'At risk' : 'Monitor',
      };
    });
    const affectedPassengers = flights.reduce((n, f) => n + f.passengers, 0);
    const missedConnections = connections
      .filter((c) => c.status === 'Missed')
      .reduce((n, c) => n + c.passengers, 0);
    const delayMinutes = flights.reduce((n, f) => n + f.propagatedDelay, 0);
    return {
      affectedFlights: flights.length,
      affectedPassengers,
      missedConnections,
      crewAffected: crewDetails.reduce((n, c) => n + (c.status === 'Monitor' ? 0 : 7), 0),
      gateConflicts: gateDetails.length,
      hotelRooms: Math.round(missedConnections * 0.22 * multiplier),
      mealVouchers: Math.round(affectedPassengers * 0.38 * multiplier),
      estimatedCompensation: Math.round(missedConnections * 420 * multiplier),
      estimatedOperationalCost: Math.round(
        (delayMinutes * 310 + affectedPassengers * 48 + missedConnections * 260) * multiplier,
      ),
      recoveryMinutes: Math.max(...flights.map((f) => f.propagatedDelay)) + 45,
      flights,
      connections,
      gateDetails,
      crewDetails,
    };
  }
}
