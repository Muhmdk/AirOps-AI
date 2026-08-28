import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { PassengerJourney } from '../models/passenger.model';
import { PassengerApiService, SEEDED_PASSENGER_JOURNEYS } from './passenger-api.service';

describe('PassengerApiService', () => {
  let http: HttpTestingController;
  let service: PassengerApiService;
  const passenger: PassengerJourney = {
    ...SEEDED_PASSENGER_JOURNEYS[0],
    riskScore: 88,
    updatedAt: '2026-08-06T13:20:00Z',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    service = TestBed.inject(PassengerApiService);
  });

  afterEach(() => http.verify());

  it('loads passenger journeys from the backend', async () => {
    const result = firstValueFrom(service.getPassengers());

    http.expectOne('/api/passengers').flush([passenger]);
    const journeys = await result;

    expect(journeys).toEqual([passenger]);
    expect(service.state()).toEqual([passenger]);
    expect(service.source()).toBe('backend');
  });

  it('loads one journey and hydrates the shared state', async () => {
    const result = firstValueFrom(service.getPassenger('PAX-001'));

    http.expectOne('/api/passengers/PAX-001').flush(passenger);
    const journey = await result;

    expect(journey.riskScore).toBe(88);
    expect(service.state()[0]).toEqual(passenger);
  });

  it('rebooks through the API and exposes the protected journey immediately', async () => {
    const rebooked: PassengerJourney = {
      ...passenger,
      status: 'Rebooked',
      riskScore: 18,
      selectedAlternativeFlight: passenger.alternativeFlights[0],
      rebookingNotes: 'Protect the party on the direct flight.',
    };
    const result = firstValueFrom(
      service.rebook(passenger.id, rebooked.selectedAlternativeFlight!, rebooked.rebookingNotes!),
    );

    const request = http.expectOne('/api/passengers/PAX-001/rebook');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      alternativeFlight: passenger.alternativeFlights[0],
      notes: 'Protect the party on the direct flight.',
    });
    request.flush(rebooked);
    const journey = await result;

    expect(journey.status).toBe('Rebooked');
    expect(service.state()[0].selectedAlternativeFlight).toBe(passenger.alternativeFlights[0]);
  });

  it('preserves backend validation errors instead of simulating success', async () => {
    const result = firstValueFrom(service.rebook('PAX-001', 'not-an-option', 'Invalid choice'));

    http.expectOne('/api/passengers/PAX-001/rebook').flush(
      { detail: 'Select one of the available alternative flights.' },
      { status: 400, statusText: 'Bad Request' },
    );

    await expect(result).rejects.toMatchObject({ status: 400 });
    expect(service.state().find(item => item.id === 'PAX-001')?.status).toBe('At risk');
  });

  it('keeps passenger operations usable when the API is unavailable', async () => {
    const result = firstValueFrom(service.getPassengers());

    http.expectOne('/api/passengers').flush('Unavailable', {
      status: 503,
      statusText: 'Service Unavailable',
    });
    const journeys = await result;

    expect(journeys).toHaveLength(SEEDED_PASSENGER_JOURNEYS.length);
    expect(service.source()).toBe('fallback');
    expect(service.connectionError()).toContain('Backend unavailable');
  });
});
