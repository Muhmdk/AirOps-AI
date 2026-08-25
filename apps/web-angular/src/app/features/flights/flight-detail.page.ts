import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { FlightsActions } from '../../store/flights/flights.actions';
import { flightsFeature } from '../../store/flights/flights.reducer';

@Component({ imports: [RouterLink], templateUrl: './flight-detail.page.html', styleUrl: './flight-detail.page.scss' })
export class FlightDetailPage {
  private readonly store = inject(Store);
  private readonly route = inject(ActivatedRoute);
  private readonly flights = this.store.selectSignal(flightsFeature.selectEntities);
  readonly loading = this.store.selectSignal(flightsFeature.selectLoading);
  readonly id = this.route.snapshot.paramMap.get('id') ?? '';
  readonly flight = computed(() => this.flights()[this.id] ?? null);
  constructor() { this.store.dispatch(FlightsActions.loadFlight({ id: this.id })); }
}
