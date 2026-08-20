import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { Store } from '@ngrx/store';
import { Flight, FlightStatus } from '../../core/models/flight.model';
import { FlightsActions } from '../../store/flights/flights.actions';
import { flightsFeature } from '../../store/flights/flights.reducer';
import { FlightCountPipe } from './flight-count.pipe';

type SortKey = 'risk' | 'departure' | 'flight';

@Component({
  imports: [CommonModule, ReactiveFormsModule, RouterLink, RouterLinkActive, FlightCountPipe],
  templateUrl: './flights.page.html',
  styleUrl: './flights.page.scss'
})
export class FlightsPage {
  private readonly store = inject(Store);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  readonly flights = this.store.selectSignal(flightsFeature.selectAll);
  readonly loading = this.store.selectSignal(flightsFeature.selectLoading);
  readonly error = this.store.selectSignal(flightsFeature.selectError);
  readonly sort = signal<SortKey>('risk');
  readonly filters = this.fb.nonNullable.group({ search: '', status: 'All', risk: 'All' });
  readonly filterValue = signal(this.filters.getRawValue());
  readonly visibleFlights = computed(() => {
    const { search, status, risk } = this.filterValue();
    const query = search.trim().toLowerCase();
    return [...this.flights()]
      .filter(f => !query || `${f.id} ${f.route} ${f.origin} ${f.destination} ${f.aircraft}`.toLowerCase().includes(query))
      .filter(f => status === 'All' || f.status === status)
      .filter(f => risk === 'All' || (risk === 'High' ? f.risk >= 65 : risk === 'Moderate' ? f.risk >= 35 && f.risk < 65 : f.risk < 35))
      .sort((a, b) => this.sort() === 'risk' ? b.risk - a.risk : this.sort() === 'departure' ? a.departure.localeCompare(b.departure) : a.id.localeCompare(b.id));
  });
  readonly statuses: Array<'All' | FlightStatus> = ['All', 'At risk', 'Delayed', 'Boarding', 'On time'];

  constructor() {
    this.store.dispatch(FlightsActions.load());
    this.filters.valueChanges.subscribe(value => this.filterValue.set({
      search: value.search ?? '', status: value.status ?? 'All', risk: value.risk ?? 'All'
    }));
  }

  openFlight(flight: Flight) { this.router.navigate(['/flights', flight.id]); }
  setStatus(status: string) { this.filters.controls.status.setValue(status); }
  clearFilters() { this.filters.reset({ search: '', status: 'All', risk: 'All' }); }
  retry() { this.store.dispatch(FlightsActions.load()); }
}
