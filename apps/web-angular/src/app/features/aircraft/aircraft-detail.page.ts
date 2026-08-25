import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AircraftOperation } from '../../core/models/aircraft.model';
import { AircraftApiService } from '../../core/services/aircraft-api.service';
import { FlightApiService } from '../../core/services/flight-api.service';

@Component({
  imports: [RouterLink],
  templateUrl: './aircraft-detail.page.html',
  styleUrl: './aircraft-detail.page.scss',
})
export class AircraftDetailPage {
  private readonly api = inject(AircraftApiService);
  private readonly flightApi = inject(FlightApiService);
  private readonly route = inject(ActivatedRoute);
  readonly registration = this.route.snapshot.paramMap.get('registration') ?? '';
  readonly aircraft = signal<AircraftOperation | null>(null);
  readonly assigned = computed(() =>
    this.flightApi.state().filter(flight => flight.aircraft.includes(this.registration))
  );
  readonly loading = signal(true);

  constructor() {
    this.api.getAircraftByRegistration(this.registration).subscribe({
      next: aircraft => this.aircraft.set(aircraft),
      error: () => this.loading.set(false),
      complete: () => this.loading.set(false),
    });
  }
}
