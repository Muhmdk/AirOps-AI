import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { FlightApiService, SEEDED_FLIGHTS } from './flight-api.service';
import { FALLBACK_NETWORK_SUMMARY, NetworkApiService } from './network-api.service';

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
});
