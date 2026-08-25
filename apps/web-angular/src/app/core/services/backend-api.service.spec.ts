import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { FlightApiService, SEEDED_FLIGHTS } from './flight-api.service';
import { FALLBACK_NETWORK_SUMMARY, NetworkApiService } from './network-api.service';
import { AirportApiService, SEEDED_AIRPORTS } from './airport-api.service';
import { AircraftApiService, SEEDED_AIRCRAFT } from './aircraft-api.service';

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
});
