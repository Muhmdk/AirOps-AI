import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AirportOperation } from '../../core/models/airport.model';
import { AirportApiService } from '../../core/services/airport-api.service';
import { FlightApiService } from '../../core/services/flight-api.service';

@Component({
  imports: [RouterLink],
  templateUrl: './airport-detail.page.html',
  styleUrl: './airport-detail.page.scss',
})
export class AirportDetailPage {
  private readonly api = inject(AirportApiService);
  private readonly flightApi = inject(FlightApiService);
  private readonly route = inject(ActivatedRoute);
  readonly code = this.route.snapshot.paramMap.get('code') ?? '';
  readonly airport = signal<AirportOperation | null>(null);
  readonly flights = computed(() =>
    this.flightApi.state().filter(flight => flight.route.includes(this.code))
  );
  readonly loading = signal(true);

  constructor() {
    this.api.getAirport(this.code).subscribe({
      next: airport => this.airport.set(airport),
      error: () => this.loading.set(false),
      complete: () => this.loading.set(false),
    });
  }
}
