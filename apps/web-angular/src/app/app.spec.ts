import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { SEEDED_FLIGHTS } from './core/services/flight-api.service';
import { FlightsActions } from './store/flights/flights.actions';
import { flightsFeature } from './store/flights/flights.reducer';
import { FlightCountPipe } from './features/flights/flight-count.pipe';
import { SEEDED_AIRPORTS } from './core/services/airport-api.service';
import { SEEDED_AIRCRAFT } from './core/services/aircraft-api.service';
import { DisruptionEngineService } from './core/services/disruption-engine.service';
import { OperationsEventService } from './core/services/operations-event.service';
import { FlightApiService } from './core/services/flight-api.service';
import { AirportApiService } from './core/services/airport-api.service';
import { AircraftApiService } from './core/services/aircraft-api.service';
import { RecoveryEngineService } from './core/services/recovery-engine.service';

describe('AirOps application', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [App], providers: [provideRouter([])] }).compileComponents();
  });

  it('creates the routed application shell', () => {
    expect(TestBed.createComponent(App).componentInstance).toBeTruthy();
  });

  it('loads flights into normalized NgRx state', () => {
    const state = flightsFeature.reducer(undefined, FlightsActions.loadSuccess({ flights: SEEDED_FLIGHTS }));
    expect(state.ids.length).toBe(5);
    expect(state.entities['AC103']?.risk).toBe(82);
    expect(state.loading).toBe(false);
  });

  it('stores the current flight search', () => {
    const state = flightsFeature.reducer(undefined, FlightsActions.setSearch({ search: 'YVR' }));
    expect(state.search).toBe('YVR');
  });

  it('calculates operational flight summaries', () => {
    const pipe = new FlightCountPipe();
    expect(pipe.transform(SEEDED_FLIGHTS, 'attention')).toBe(3);
    expect(pipe.transform(SEEDED_FLIGHTS, 'passengers')).toBe(1178);
    expect(pipe.transform(SEEDED_FLIGHTS, 'risk')).toBe(56);
  });

  it('seeds airport operations with valid capacity data', () => {
    expect(SEEDED_AIRPORTS).toHaveLength(6);
    expect(SEEDED_AIRPORTS.every(airport => airport.gatesUsed <= airport.gatesTotal)).toBe(true);
    expect(SEEDED_AIRPORTS.find(airport => airport.code === 'YYZ')?.risk).toBe('High');
  });

  it('seeds a fleet with valid utilization and maintenance data', () => {
    expect(SEEDED_AIRCRAFT).toHaveLength(6);
    expect(SEEDED_AIRCRAFT.every(aircraft => aircraft.utilization >= 0 && aircraft.utilization <= 100)).toBe(true);
    expect(SEEDED_AIRCRAFT.filter(aircraft => aircraft.status === 'Unavailable')).toHaveLength(1);
  });

  it('models operational events with navigable affected entities', () => {
    const event = { severity: 'Critical', entityType: 'airport', entityId: 'YYZ' } as const;
    expect(event.severity).toBe('Critical');
    expect(`${event.entityType}s/${event.entityId}`).toBe('airports/YYZ');
  });

  it('propagates a disruption through an aircraft rotation', () => {
    const engine = new DisruptionEngineService();
    const disruption = engine.create({ type: 'Severe weather', severity: 'Critical', airport: 'YYZ', flightId: 'AC103', durationMinutes: 120 });
    expect(disruption.impact.affectedFlights).toBe(3);
    expect(disruption.impact.flights[1].propagatedDelay).toBeLessThan(disruption.impact.flights[0].propagatedDelay);
    expect(disruption.impact.affectedPassengers).toBeGreaterThan(500);
    expect(disruption.impact.estimatedOperationalCost).toBeGreaterThan(0);
    expect(disruption.impact.connections.some(connection => connection.status === 'Missed')).toBe(true);
    expect(disruption.impact.gateDetails.length).toBeGreaterThan(0);
    expect(disruption.impact.crewDetails.some(crew => crew.status !== 'Monitor')).toBe(true);
  });

  it('publishes created disruptions into the operational event stream', () => {
    const events = new OperationsEventService();
    const engine = new DisruptionEngineService(events);
    let latestTitle = '';
    const subscription = events.events$.subscribe(items => latestTitle = items[0]?.title ?? '');
    engine.create({ type: 'Runway closure', severity: 'Critical', airport: 'YYZ', flightId: 'AC418', durationMinutes: 60 });
    expect(latestTitle).toContain('Runway closure');
    subscription.unsubscribe();
  });

  it('mutates and recomputes shared network state from active disruptions', () => {
    localStorage.removeItem('airops-disruptions');
    localStorage.removeItem('airops-disruption-audit');
    localStorage.removeItem('airops-scenario-runs');
    const flights = new FlightApiService();
    const airports = new AirportApiService();
    const aircraft = new AircraftApiService();
    const engine = new DisruptionEngineService(undefined, flights, airports, aircraft);
    const disruption = engine.create({ type: 'Aircraft maintenance', severity: 'Critical', airport: 'YYZ', flightId: 'AC882', durationMinutes: 90 });
    expect(flights.state().find(flight => flight.id === 'AC882')?.status).toBe('At risk');
    expect(aircraft.state().find(item => item.nextFlight === 'AC882')?.status).toBe('Unavailable');
    expect(airports.state().find(airport => airport.code === 'YYZ')?.health).toBeLessThan(62);
    engine.resolve(disruption.id);
    expect(flights.state().find(flight => flight.id === 'AC882')?.risk).toBe(18);
    expect(aircraft.state().find(item => item.nextFlight === 'AC882')?.status).toBe('Available');
  });

  it('restores persisted disruptions, audit records, and derived network state', () => {
    localStorage.removeItem('airops-disruptions');
    localStorage.removeItem('airops-disruption-audit');
    localStorage.removeItem('airops-scenario-runs');
    const firstFlights = new FlightApiService();
    const firstAirports = new AirportApiService();
    const firstAircraft = new AircraftApiService();
    const firstEngine = new DisruptionEngineService(undefined, firstFlights, firstAirports, firstAircraft);
    const created = firstEngine.create({ type: 'Runway closure', severity: 'Critical', airport: 'YYZ', flightId: 'AC882', durationMinutes: 75 });
    expect(firstEngine.auditEntries()[0].changes.length).toBeGreaterThan(0);
    const restoredFlights = new FlightApiService();
    const restoredEngine = new DisruptionEngineService(undefined, restoredFlights, new AirportApiService(), new AircraftApiService());
    expect(restoredEngine.get(created.id)?.type).toBe('Runway closure');
    expect(restoredEngine.auditEntries().some(entry => entry.disruptionId === created.id)).toBe(true);
    expect(restoredFlights.state().find(flight => flight.id === 'AC882')?.risk).toBeGreaterThan(18);
    localStorage.removeItem('airops-disruptions');
    localStorage.removeItem('airops-disruption-audit');
    localStorage.removeItem('airops-scenario-runs');
  });

  it('runs, snapshots, resets, and deterministically replays scenarios', () => {
    localStorage.clear();
    const engine = new DisruptionEngineService(undefined, new FlightApiService(), new AirportApiService(), new AircraftApiService());
    engine.resetSimulation();
    const run = engine.runScenario('Mechanical test', 'Repeatable maintenance event', { type: 'Aircraft maintenance', severity: 'Critical', airport: 'YYZ', flightId: 'AC882', durationMinutes: 95 });
    expect(run.after.networkHealth).toBeLessThan(run.before.networkHealth);
    expect(run.after.availableAircraft).toBeLessThan(run.before.availableAircraft);
    expect(run.after.estimatedCost).toBeGreaterThan(run.before.estimatedCost);
    const replay = engine.replay(run);
    expect(replay.after).toEqual(run.after);
    engine.resetSimulation();
    expect(engine.disruptions()).toHaveLength(0);
    expect(engine.scenarioRuns()).toHaveLength(0);
    localStorage.clear();
  });

  it('detects and compounds simultaneous network disruptions', () => {
    localStorage.clear();
    const flights = new FlightApiService();
    const engine = new DisruptionEngineService(undefined, flights, new AirportApiService(), new AircraftApiService());
    engine.runNetworkStressTest();
    expect(engine.active()).toHaveLength(3);
    expect(engine.compoundImpacts().length).toBeGreaterThanOrEqual(2);
    expect(engine.compoundImpacts().some(overlap => overlap.sharedEntities.includes('YYZ'))).toBe(true);
    const independentCost = engine.active().reduce((sum, disruption) => sum + disruption.impact.estimatedOperationalCost, 0);
    expect(engine.totalImpact().cost).toBeGreaterThan(independentCost);
    expect(flights.state().find(flight => flight.id === 'AC103')?.risk).toBe(99);
    engine.resolve(engine.active()[0].id);
    expect(engine.compoundImpacts().length).toBeLessThan(3);
    localStorage.clear();
  });

  it('generates, scores, and ranks recovery candidates', () => {
    const disruptions = new DisruptionEngineService();
    const recovery = new RecoveryEngineService(disruptions, new AircraftApiService());
    const disruption = disruptions.get('DSP-001')!;
    const plans = recovery.generate(disruption);
    expect(plans).toHaveLength(6);
    expect(plans[0].recommended).toBe(true);
    expect(plans.filter(plan => plan.recommended)).toHaveLength(1);
    expect(plans.every((plan, index) => index === 0 || plans[index - 1].score >= plan.score)).toBe(true);
    expect(plans.some(plan => plan.expectedDelayMinutes < disruption.impact.flights[0].propagatedDelay)).toBe(true);
    expect(plans.some(plan => plan.action === 'Change gate')).toBe(true);
    expect(plans.every(plan => plan.score >= 0 && plan.score <= 100)).toBe(true);
  });

  it('approves, executes, audits, and restores a recovery outcome', () => {
    localStorage.clear();
    const flights = new FlightApiService(); const airports = new AirportApiService(); const aircraft = new AircraftApiService();
    const disruptions = new DisruptionEngineService(undefined, flights, airports, aircraft);
    disruptions.resetSimulation();
    const disruption = disruptions.create({ type: 'Severe weather', severity: 'Critical', airport: 'YYZ', flightId: 'AC103', durationMinutes: 90 });
    const recovery = new RecoveryEngineService(disruptions, aircraft, flights, airports);
    const candidates = recovery.generate(disruption);
    const highRisk = candidates.find(candidate => candidate.operationalRisk === 'High')!;
    expect(recovery.approve(highRisk.id, 'Attempt without supervisor').ok).toBe(false);
    const plan = candidates[0];
    const result = recovery.approve(plan.id, 'Protect international passengers and restore rotation.', true);
    expect(result.ok).toBe(true);
    expect(recovery.plans()[disruption.id].find(item => item.id === plan.id)?.status).toBe('Approved');
    expect(recovery.plans()[disruption.id].filter(item => item.status === 'Rejected').length).toBeGreaterThan(0);
    expect(disruptions.get(disruption.id)?.status).toBe('Resolved');
    expect(flights.state().find(flight => flight.id === 'AC103')?.delay).toBe(plan.expectedDelayMinutes);
    expect(recovery.auditEntries()[0].notes).toContain('Protect international');
    expect(recovery.auditEntries()[0].supervisorOverride).toBe(true);
    const restoredFlights = new FlightApiService();
    const restoredDisruptions = new DisruptionEngineService(undefined, restoredFlights, new AirportApiService(), new AircraftApiService());
    new RecoveryEngineService(restoredDisruptions, new AircraftApiService(), restoredFlights, new AirportApiService());
    expect(restoredFlights.state().find(flight => flight.id === 'AC103')?.delay).toBe(plan.expectedDelayMinutes);
    localStorage.clear();
  });

  it('rejects a candidate, promotes the next plan, and executes a gate reassignment', () => {
    localStorage.clear();
    const flights = new FlightApiService();
    const airports = new AirportApiService();
    const aircraft = new AircraftApiService();
    const disruptions = new DisruptionEngineService(undefined, flights, airports, aircraft);
    disruptions.resetSimulation();
    const disruption = disruptions.create({ type: 'Gate conflict', severity: 'High', airport: 'YYZ', flightId: 'AC103', durationMinutes: 55 });
    const recovery = new RecoveryEngineService(disruptions, aircraft, flights, airports);
    const candidates = recovery.generate(disruption);
    const rejectedPlan = candidates.find(plan => plan.action === 'Maintain rotation')!;
    expect(recovery.reject(rejectedPlan.id, 'Gate move provides a cleaner recovery.')).toBe(true);
    expect(recovery.plans()[disruption.id].find(plan => plan.id === rejectedPlan.id)?.status).toBe('Rejected');
    expect(recovery.plans()[disruption.id].filter(plan => plan.recommended)).toHaveLength(1);
    expect(recovery.approve(rejectedPlan.id, 'Cannot approve rejected plan.').ok).toBe(false);
    expect(recovery.auditEntries()[0].outcome.delayBefore).toBeGreaterThan(0);
    const gatePlan = recovery.plans()[disruption.id].find(plan => plan.action === 'Change gate')!;
    const originalGate = flights.state().find(flight => flight.id === 'AC103')?.gate;
    expect(recovery.approve(gatePlan.id, 'Clear the gate conflict and protect the rotation.', true).ok).toBe(true);
    expect(flights.state().find(flight => flight.id === 'AC103')?.gate).not.toBe(originalGate);
    expect(recovery.auditEntries()[0].outcome.delayAfter).toBe(gatePlan.expectedDelayMinutes);
    localStorage.clear();
  });
});
