import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { DisruptionApiService } from '../../core/services/disruption-api.service';
import { DisruptionEngineService } from '../../core/services/disruption-engine.service';
import { FlightsActions } from '../../store/flights/flights.actions';
import { flightsFeature } from '../../store/flights/flights.reducer';

@Component({ imports: [RouterLink], templateUrl: './flight-detail.page.html', styleUrl: './flight-detail.page.scss' })
export class FlightDetailPage {
  private readonly store = inject(Store);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly disruptionApi = inject(DisruptionApiService);
  private readonly disruptionEngine = inject(DisruptionEngineService);
  private readonly flights = this.store.selectSignal(flightsFeature.selectEntities);
  readonly loading = this.store.selectSignal(flightsFeature.selectLoading);
  readonly id = this.route.snapshot.paramMap.get('id') ?? '';
  readonly flight = computed(() => this.flights()[this.id] ?? null);
  readonly relatedDisruption = computed(() => {
    const matches = this.disruptionEngine.disruptions().filter(disruption =>
      disruption.primaryFlight === this.id ||
      disruption.impact.flights.some(flight => flight.id === this.id)
    );
    return matches.find(disruption => disruption.status !== 'Resolved') ?? matches[0] ?? null;
  });

  constructor() {
    this.store.dispatch(FlightsActions.loadFlight({ id: this.id }));
    this.disruptionApi.getDisruptions().subscribe({ error: () => undefined });
  }

  openDisruption() {
    const disruption = this.relatedDisruption();
    return disruption
      ? this.router.navigate(['/disruptions', disruption.id])
      : this.router.navigate(['/disruptions']);
  }

  evaluateRecovery() {
    const disruption = this.relatedDisruption();
    return disruption
      ? this.router.navigate(['/recovery-plans', disruption.id])
      : this.router.navigate(['/recovery-plans']);
  }
}
