import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PassengerJourney, PassengerJourneyStatus } from '../../core/models/passenger.model';
import { PassengerApiService } from '../../core/services/passenger-api.service';

@Component({
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './passengers.page.html',
  styleUrl: './passengers.page.scss',
})
export class PassengersPage {
  readonly api = inject(PassengerApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  readonly journeys = signal<PassengerJourney[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly search = signal('');
  readonly status = signal<'All' | PassengerJourneyStatus>('All');
  readonly flight = signal(this.route.snapshot.queryParamMap.get('flightId') ?? 'All');
  readonly statuses: Array<'All' | PassengerJourneyStatus> =
    ['All', 'At risk', 'Misconnected', 'Protected', 'Rebooked'];
  readonly flights = computed(() =>
    [...new Set([
      ...(this.flight() === 'All' ? [] : [this.flight()]),
      ...this.journeys().map(item => item.currentFlightId),
    ])].sort());
  readonly visible = computed(() => {
    const query = this.search().trim().toLowerCase();
    return this.journeys().filter(item =>
      (!query || `${item.leadPassenger} ${item.bookingReference} ${item.currentFlightId} ${item.connectingFlightId} ${item.originCode} ${item.destinationCode}`.toLowerCase().includes(query)) &&
      (this.status() === 'All' || item.status === this.status()) &&
      (this.flight() === 'All' || item.currentFlightId === this.flight()));
  });
  readonly totals = computed(() => ({
    travelers: this.journeys().reduce((sum, item) => sum + item.partySize, 0),
    atRisk: this.journeys().filter(item => item.status === 'At risk' || item.status === 'Misconnected')
      .reduce((sum, item) => sum + item.partySize, 0),
    misconnected: this.journeys().filter(item => item.status === 'Misconnected')
      .reduce((sum, item) => sum + item.partySize, 0),
    rebooked: this.journeys().filter(item => item.status === 'Rebooked')
      .reduce((sum, item) => sum + item.partySize, 0),
    careCost: this.journeys().reduce((sum, item) => sum + item.estimatedCareCost, 0),
  }));

  constructor() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set('');
    this.api.getPassengers().subscribe({
      next: journeys => this.journeys.set(journeys),
      error: () => {
        this.error.set('Passenger journeys could not be loaded.');
        this.loading.set(false);
      },
      complete: () => this.loading.set(false),
    });
  }

  open(journey: PassengerJourney) { this.router.navigate(['/passengers', journey.id]); }
  clear() { this.search.set(''); this.status.set('All'); this.flight.set('All'); }
}
