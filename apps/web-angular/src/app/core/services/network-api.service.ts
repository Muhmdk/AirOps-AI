import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { NetworkSummary } from '../models/network-summary.model';

export const FALLBACK_NETWORK_SUMMARY: NetworkSummary = {
  flightsToday: 5,
  onTime: 1,
  delayed: 1,
  boarding: 1,
  atRisk: 2,
  cancelled: 0,
  highRisk: 2,
  passengers: 1178,
  connectingPassengers: 202,
  networkHealth: 83,
  airportsMonitored: 6,
  airportAverageDelay: 17,
  aircraftAvailable: 5,
  aircraftUnavailable: 1,
};

@Injectable({ providedIn: 'root' })
export class NetworkApiService {
  readonly state = signal<NetworkSummary>({ ...FALLBACK_NETWORK_SUMMARY });
  readonly source = signal<'loading' | 'backend' | 'fallback'>('fallback');

  constructor(private readonly http?: HttpClient) {}

  load() {
    if (!this.http) return;
    this.source.set('loading');
    this.http.get<NetworkSummary>('/api/network/summary').subscribe({
      next: summary => {
        this.state.set(summary);
        this.source.set('backend');
      },
      error: () => this.source.set('fallback'),
    });
  }
}
