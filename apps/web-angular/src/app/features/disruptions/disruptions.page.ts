import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  Disruption,
  DisruptionSeverity,
  DisruptionType,
} from '../../core/models/disruption.model';
import { DisruptionApiService } from '../../core/services/disruption-api.service';
import { DisruptionEngineService } from '../../core/services/disruption-engine.service';
import { FlightApiService } from '../../core/services/flight-api.service';

@Component({
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './disruptions.page.html',
  styleUrl: './disruptions.page.scss',
})
export class DisruptionsPage {
  readonly engine = inject(DisruptionEngineService);
  readonly api = inject(DisruptionApiService);
  private readonly flightApi = inject(FlightApiService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  readonly showCreate = signal(false);
  readonly status = signal('Active');
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal('');
  readonly visible = computed(() => this.engine.disruptions().filter(disruption =>
    this.status() === 'All' || disruption.status === this.status()));
  readonly types: DisruptionType[] = [
    'Severe weather', 'Aircraft maintenance', 'Late incoming aircraft', 'Gate conflict',
    'Airport congestion', 'Crew timing issue', 'Runway closure', 'Air traffic restriction',
  ];
  readonly flights = this.flightApi.state;
  readonly form = this.fb.nonNullable.group({
    type: ['Severe weather' as DisruptionType, Validators.required],
    severity: ['High' as DisruptionSeverity, Validators.required],
    airport: ['YYZ', Validators.required],
    flightId: ['AC103', Validators.required],
    durationMinutes: [90, [Validators.required, Validators.min(15)]],
  });

  constructor() {
    this.api.getDisruptions().subscribe({
      error: () => this.loading.set(false),
      complete: () => this.loading.set(false),
    });
  }

  create() {
    if (this.form.invalid || this.submitting()) return;
    this.submitting.set(true);
    this.error.set('');
    this.api.create(this.form.getRawValue()).subscribe({
      next: disruption => {
        this.showCreate.set(false);
        this.router.navigate(['/disruptions', disruption.id]);
      },
      error: () => {
        this.error.set('Unable to create this disruption. Check the scenario and try again.');
        this.submitting.set(false);
      },
      complete: () => this.submitting.set(false),
    });
  }

  open(disruption: Disruption) {
    this.router.navigate(['/disruptions', disruption.id]);
  }
}
