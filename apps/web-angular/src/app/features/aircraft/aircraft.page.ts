import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AircraftOperation, AircraftStatus } from '../../core/models/aircraft.model';
import { AircraftApiService } from '../../core/services/aircraft-api.service';

@Component({
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './aircraft.page.html',
  styleUrl: './aircraft.page.scss',
})
export class AircraftPage {
  private readonly api = inject(AircraftApiService);
  private readonly router = inject(Router);
  readonly fleet = signal<AircraftOperation[]>([]);
  readonly loading = signal(true);
  readonly search = signal('');
  readonly status = signal<'All' | AircraftStatus>('All');
  readonly family = signal('All');
  readonly dataSource = this.api.source;
  readonly visible = computed(() => {
    const query = this.search().toLowerCase().trim();
    return this.fleet()
      .filter(aircraft =>
        (!query || `${aircraft.registration} ${aircraft.type} ${aircraft.location} ${aircraft.nextFlight}`
          .toLowerCase().includes(query)) &&
        (this.status() === 'All' || aircraft.status === this.status()) &&
        (this.family() === 'All' || aircraft.family === this.family())
      )
      .sort((first, second) => first.health - second.health);
  });
  readonly totals = computed(() => ({
    available: this.fleet().filter(aircraft => aircraft.status !== 'Unavailable').length,
    unavailable: this.fleet().filter(aircraft => aircraft.status === 'Unavailable').length,
    utilization: Math.round(this.fleet().reduce(
      (total, aircraft) => total + aircraft.utilization, 0) / (this.fleet().length || 1)),
    maintenance: this.fleet().filter(aircraft => aircraft.maintenanceDue < 50).length,
  }));

  constructor() {
    this.api.getAircraft().subscribe(items => {
      this.fleet.set(items);
      this.loading.set(false);
    });
  }

  open(aircraft: AircraftOperation) {
    this.router.navigate(['/aircraft', aircraft.registration]);
  }

  clear() {
    this.search.set('');
    this.status.set('All');
    this.family.set('All');
  }
}
