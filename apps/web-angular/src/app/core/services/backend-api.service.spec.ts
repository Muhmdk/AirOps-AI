import { HttpClient, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { FlightApiService, SEEDED_FLIGHTS } from './flight-api.service';
import { FALLBACK_NETWORK_SUMMARY, NetworkApiService } from './network-api.service';
import { AirportApiService, SEEDED_AIRPORTS } from './airport-api.service';
import { AircraftApiService, SEEDED_AIRCRAFT } from './aircraft-api.service';
import { DisruptionApiService } from './disruption-api.service';
import { DisruptionEngineService } from './disruption-engine.service';
import { RecoveryApiService } from './recovery-api.service';
import { RecoveryEngineService } from './recovery-engine.service';

const API_FLIGHT = {
  id: 'AC103',
  route: 'YYZ → YVR',
  originCode: 'YYZ',
  origin: 'Toronto',
  destinationCode: 'YVR',
  destination: 'Vancouver',
  scheduledDeparture: '2026-08-06T09:15:00-04:00',
  estimatedDeparture: '2026-08-06T10:23:00-04:00',
  scheduledArrival: '2026-08-06T11:28:00-07:00',
  estimatedArrival: '2026-08-06T12:36:00-07:00',
  aircraftRegistration: 'C-FVLX',
  aircraftType: 'Boeing 787-9',
  gate: 'D24',
  status: 'AtRisk',
  risk: 82,
  delayMinutes: 68,
  passengers: 286,
  connectingPassengers: 47,
  riskLabel: 'Severe weather',
};

describe('backend API adapters', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('maps backend flight contracts into the existing UI model', async () => {
    const service = TestBed.inject(FlightApiService);
    const result = firstValueFrom(service.getFlights());

    http.expectOne('/api/flights').flush([API_FLIGHT]);
    const flights = await result;

    expect(flights[0]).toMatchObject({
      id: 'AC103',
      departure: '09:15',
      arrival: '12:36',
      aircraft: 'Boeing 787-9 · C-FVLX',
      status: 'At risk',
      connections: 47,
    });
    expect(service.source()).toBe('backend');
  });

  it('loads direct flight details and updates shared flight state', async () => {
    const service = TestBed.inject(FlightApiService);
    const result = firstValueFrom(service.getFlight('ac103'));

    http.expectOne('/api/flights/ac103').flush(API_FLIGHT);
    const flight = await result;

    expect(flight.id).toBe('AC103');
    expect(service.state()[0].delay).toBe(68);
  });

  it('keeps the application usable with seeded flights while offline', async () => {
    const service = TestBed.inject(FlightApiService);
    const result = firstValueFrom(service.getFlights());

    http.expectOne('/api/flights').flush('Unavailable', {
      status: 503,
      statusText: 'Service Unavailable',
    });
    const flights = await result;

    expect(flights).toHaveLength(SEEDED_FLIGHTS.length);
    expect(service.source()).toBe('fallback');
    expect(service.connectionError()).toContain('Backend unavailable');
  });

  it('loads network summary metrics and retains a fallback on failure', () => {
    const service = TestBed.inject(NetworkApiService);
    service.load();
    http.expectOne('/api/network/summary').flush({
      ...FALLBACK_NETWORK_SUMMARY,
      networkHealth: 76,
      aircraftUnavailable: 2,
    });

    expect(service.state().networkHealth).toBe(76);
    expect(service.source()).toBe('backend');

    service.load();
    http.expectOne('/api/network/summary').flush('Unavailable', {
      status: 503,
      statusText: 'Service Unavailable',
    });

    expect(service.state().networkHealth).toBe(76);
    expect(service.source()).toBe('fallback');
  });

  it('loads airport operations from the backend contract', async () => {
    const service = TestBed.inject(AirportApiService);
    const result = firstValueFrom(service.getAirports());

    http.expectOne('/api/airports').flush([
      { ...SEEDED_AIRPORTS[0], health: 59, weather: 'Severe thunderstorms' },
    ]);
    const airports = await result;

    expect(airports[0]).toMatchObject({ code: 'YYZ', health: 59 });
    expect(service.state()[0].weather).toBe('Severe thunderstorms');
    expect(service.source()).toBe('backend');
  });

  it('loads airport details independently and falls back while offline', async () => {
    const service = TestBed.inject(AirportApiService);
    const result = firstValueFrom(service.getAirport('yyz'));

    http.expectOne('/api/airports/yyz').flush('Unavailable', {
      status: 503,
      statusText: 'Service Unavailable',
    });
    const airport = await result;

    expect(airport.code).toBe('YYZ');
    expect(service.source()).toBe('fallback');
    expect(service.connectionError()).toContain('Backend unavailable');
  });

  it('loads aircraft operations from the backend contract', async () => {
    const service = TestBed.inject(AircraftApiService);
    const result = firstValueFrom(service.getAircraft());

    http.expectOne('/api/aircraft').flush([
      { ...SEEDED_AIRCRAFT[0], utilization: 92, maintenanceDue: 120 },
    ]);
    const fleet = await result;

    expect(fleet[0]).toMatchObject({ registration: 'C-FVLX', utilization: 92 });
    expect(service.state()[0].maintenanceDue).toBe(120);
    expect(service.source()).toBe('backend');
  });

  it('loads aircraft details independently and falls back while offline', async () => {
    const service = TestBed.inject(AircraftApiService);
    const result = firstValueFrom(service.getAircraftByRegistration('c-fvlx'));

    http.expectOne('/api/aircraft/c-fvlx').flush('Unavailable', {
      status: 503,
      statusText: 'Service Unavailable',
    });
    const aircraft = await result;

    expect(aircraft.registration).toBe('C-FVLX');
    expect(service.source()).toBe('fallback');
    expect(service.connectionError()).toContain('Backend unavailable');
  });

  it('hydrates backend disruptions and adapts simulation timestamps', async () => {
    const engine = new DisruptionEngineService();
    const service = new DisruptionApiService(engine, TestBed.inject(HttpClient));
    const backend = {
      ...engine.disruptions()[0],
      startedAt: '2026-08-06T09:08:00-04:00',
      createdAt: '2026-08-06T09:08:00-04:00',
      resolvedAt: null,
    };
    const result = firstValueFrom(service.getDisruptions());

    http.expectOne('/api/disruptions').flush([backend]);
    const disruptions = await result;

    expect(disruptions[0].startedAt).toBe('09:08');
    expect(engine.disruptions()).toHaveLength(1);
    expect(service.source()).toBe('backend');
  });

  it('creates disruptions through the backend workflow', async () => {
    const engine = new DisruptionEngineService();
    const service = new DisruptionApiService(engine, TestBed.inject(HttpClient));
    const scenario = {
      type: 'Runway closure' as const,
      severity: 'Critical' as const,
      airport: 'YYZ',
      flightId: 'AC103',
      durationMinutes: 75,
    };
    const result = firstValueFrom(service.create(scenario));
    const request = http.expectOne('/api/disruptions');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(scenario);
    request.flush({
      ...engine.disruptions()[0],
      ...scenario,
      id: 'DSP-003',
      primaryFlight: scenario.flightId,
      startedAt: '2026-08-06T09:20:00-04:00',
      createdAt: '2026-08-06T09:20:00-04:00',
      resolvedAt: null,
    });

    expect((await result).id).toBe('DSP-003');
    expect(engine.get('DSP-003')).toBeTruthy();
  });

  it('loads disruption audit and resolves through backend endpoints', async () => {
    const engine = new DisruptionEngineService();
    const service = new DisruptionApiService(engine, TestBed.inject(HttpClient));
    const auditResult = firstValueFrom(service.getAudit('DSP-001'));
    http.expectOne('/api/disruptions/DSP-001/audit').flush([{
      id: '3eb851e2-1a2f-4c74-81c5-1c40f7457511',
      disruptionId: 'DSP-001',
      action: 'Created',
      actor: 'Maya Chen',
      timestamp: '2026-08-06T09:08:00-04:00',
      summary: 'Created critical severe weather disruption',
      changes: [{ entityType: 'Flight', entityId: 'AC103', field: 'Risk', before: '82', after: '99' }],
    }]);
    expect(await auditResult).toHaveLength(1);
    expect(engine.auditEntries()[0].changes[0].field).toBe('Risk');

    const resolveResult = firstValueFrom(service.resolve('DSP-001'));
    http.expectOne('/api/disruptions/DSP-001/resolve').flush({
      ...engine.get('DSP-001'),
      status: 'Resolved',
      startedAt: '2026-08-06T09:08:00-04:00',
      resolvedAt: '2026-08-06T10:15:00-04:00',
    });
    expect((await resolveResult).status).toBe('Resolved');
  });

  it('uses the browser disruption engine when the backend is unavailable', async () => {
    const engine = new DisruptionEngineService();
    const service = new DisruptionApiService(engine, TestBed.inject(HttpClient));
    const before = engine.disruptions().length;
    const result = firstValueFrom(service.create({
      type: 'Gate conflict',
      severity: 'High',
      airport: 'YYZ',
      flightId: 'AC418',
      durationMinutes: 45,
    }));

    http.expectOne('/api/disruptions').flush('Unavailable', {
      status: 503,
      statusText: 'Service Unavailable',
    });

    expect((await result).type).toBe('Gate conflict');
    expect(engine.disruptions()).toHaveLength(before + 1);
    expect(service.source()).toBe('fallback');
  });

  it('generates and hydrates backend recovery candidates', async () => {
    const disruptions = new DisruptionEngineService();
    const recovery = new RecoveryEngineService(disruptions, new AircraftApiService());
    const service = new RecoveryApiService(recovery, TestBed.inject(HttpClient));
    const plan = recovery.generate(disruptions.get('DSP-001')!)[0];
    recovery.hydratePlans('DSP-001', []);
    const result = firstValueFrom(service.generate('DSP-001'));

    const request = http.expectOne('/api/disruptions/DSP-001/recovery-plans/generate');
    expect(request.request.method).toBe('POST');
    request.flush([{ ...plan, requiresSupervisor: false }]);
    const plans = await result;

    expect(plans).toHaveLength(1);
    expect(recovery.plans()['DSP-001'][0].id).toBe(plan.id);
    expect(service.source()).toBe('backend');
  });

  it('approves a plan and refreshes all backend candidate statuses', async () => {
    const disruptions = new DisruptionEngineService();
    const recovery = new RecoveryEngineService(disruptions, new AircraftApiService());
    const service = new RecoveryApiService(recovery, TestBed.inject(HttpClient));
    const candidates = recovery.generate(disruptions.get('DSP-001')!);
    const selected = candidates[0];
    const approved = { ...selected, status: 'Approved' as const };
    const rejected = candidates.slice(1).map(plan => ({
      ...plan,
      status: 'Rejected' as const,
      recommended: false,
    }));
    const audit = {
      id: 'f8ad3828-bb7c-4c99-88db-f921112cad93',
      planId: selected.id,
      disruptionId: selected.disruptionId,
      action: 'Approved' as const,
      actor: 'Maya Chen',
      actorRole: 'Operations Controller',
      timestamp: '2026-08-06T09:30:00-04:00',
      notes: 'Protect the rotation.',
      supervisorOverride: false,
      outcome: { delayBefore: 91, delayAfter: 31, costBefore: 100000, costAfter: 62000, missedBefore: 59, missedAfter: 14 },
    };
    const result = firstValueFrom(service.approve(selected, audit.notes, false));

    const decision = http.expectOne(`/api/recovery-plans/${selected.id}/approve`);
    expect(decision.request.body).toEqual({ notes: audit.notes, supervisorOverride: false });
    decision.flush({ plan: approved, audit });
    http.expectOne('/api/disruptions/DSP-001/recovery-plans').flush([approved, ...rejected]);
    await result;

    expect(recovery.getPlan(selected.id)?.status).toBe('Approved');
    expect(recovery.plans()['DSP-001'].filter(plan => plan.status === 'Rejected')).toHaveLength(5);
    expect(recovery.auditEntries()[0].notes).toBe('Protect the rotation.');
  });

  it('loads the immutable backend recovery decision log', async () => {
    const disruptions = new DisruptionEngineService();
    const recovery = new RecoveryEngineService(disruptions, new AircraftApiService());
    const service = new RecoveryApiService(recovery, TestBed.inject(HttpClient));
    const result = firstValueFrom(service.getDecisionLog());
    const entry = {
      id: 'e661c4c2-c69f-4ba0-9380-aa1d991c52f1',
      planId: 'RCP-001-2',
      disruptionId: 'DSP-001',
      action: 'Rejected' as const,
      actor: 'Maya Chen',
      actorRole: 'Operations Controller',
      timestamp: '2026-08-06T09:25:00-04:00',
      notes: 'Insufficient connection protection.',
      supervisorOverride: false,
      outcome: { delayBefore: 91, delayAfter: 91, costBefore: 100000, costAfter: 100000, missedBefore: 59, missedAfter: 59 },
    };

    http.expectOne('/api/recovery-decisions').flush([entry]);

    expect(await result).toEqual([entry]);
    expect(recovery.auditEntries()[0].action).toBe('Rejected');
  });

  it('generates recovery plans locally during backend outages', async () => {
    const disruptions = new DisruptionEngineService();
    const recovery = new RecoveryEngineService(disruptions, new AircraftApiService());
    const service = new RecoveryApiService(recovery, TestBed.inject(HttpClient));
    const result = firstValueFrom(service.generate('DSP-001'));

    http.expectOne('/api/disruptions/DSP-001/recovery-plans/generate').flush('Unavailable', {
      status: 503,
      statusText: 'Service Unavailable',
    });

    expect((await result).length).toBeGreaterThanOrEqual(3);
    expect(service.source()).toBe('fallback');
  });
});
