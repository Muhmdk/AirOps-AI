import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Store } from '@ngrx/store';
import { Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { Flight, OperationalEvent } from '../../core/models/flight.model';
import { OperationsEventService } from '../../core/services/operations-event.service';
import { FlightsActions } from '../../store/flights/flights.actions';
import { flightsFeature } from '../../store/flights/flights.reducer';
import { AuthService } from '../../core/services/auth.service';
import { NetworkApiService } from '../../core/services/network-api.service';

@Component({
  selector: 'app-overview-page',
  imports: [CommonModule, FormsModule],
  templateUrl: './overview.page.html',
  styleUrl: './overview.page.scss'
})
export class OverviewPage {
  private readonly store = inject(Store);
  private readonly router = inject(Router);
  private readonly operations = inject(OperationsEventService);
  readonly auth = inject(AuthService);
  private readonly networkApi = inject(NetworkApiService);
  readonly flights = this.store.selectSignal(flightsFeature.selectAll);
  readonly filteredFlights = this.store.selectSignal(flightsFeature.selectFilteredFlights);
  readonly selected = this.store.selectSignal(flightsFeature.selectSelectedFlight);
  readonly events = toSignal(this.operations.events$, { initialValue: [] as OperationalEvent[] });
  readonly recentEvents = computed(() => this.events().slice(0, 4));
  readonly search = this.store.selectSignal(flightsFeature.selectSearch);
  readonly networkSource = this.networkApi.source;
  readonly networkMetrics = computed(() => {
    const summary = this.networkApi.state();
    return {
      health: summary.networkHealth,
      onTime: summary.onTime,
      delayed: summary.delayed,
      atRisk: summary.atRisk,
      passengers: summary.passengers,
      connections: summary.connectingPassengers,
      highRisk: summary.highRisk,
      fleetAvailable: summary.aircraftAvailable,
      fleetTotal: summary.aircraftAvailable + summary.aircraftUnavailable,
    };
  });
  activeNav = signal('Overview');
  toast = signal('');
  constructor() {
    this.store.dispatch(FlightsActions.load());
    this.networkApi.load();
  }
  setSearch(search: string) { this.store.dispatch(FlightsActions.setSearch({ search })); }
  selectFlight(flight: Flight) { this.router.navigate(['/flights', flight.id]); }
  closeDetail() { this.store.dispatch(FlightsActions.select({ id: null })); }
  navTo(item: string) {
    this.activeNav.set(item);
    const route = item.toLowerCase().replaceAll(' ', '-');
    if (item !== 'Overview') this.router.navigate(['/', route]);
  }
  notify(message: string) {
    this.toast.set(message);
    setTimeout(() => this.toast.set(''), 2600);
  }
  signOut() { this.auth.signOut(); }
}
