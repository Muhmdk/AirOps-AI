import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PassengerJourney } from '../../core/models/passenger.model';
import { PassengerApiService } from '../../core/services/passenger-api.service';

@Component({
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './passenger-detail.page.html',
  styleUrl: './passenger-detail.page.scss',
})
export class PassengerDetailPage {
  readonly api = inject(PassengerApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  readonly id = this.route.snapshot.paramMap.get('id') ?? '';
  readonly journey = signal<PassengerJourney | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal('');
  readonly showRebook = signal(false);
  readonly submitting = signal(false);
  readonly actionError = signal('');
  readonly success = signal('');
  readonly form = this.fb.nonNullable.group({
    alternativeFlight: ['', Validators.required],
    notes: ['', [Validators.required, Validators.minLength(12)]],
  });

  constructor() {
    this.api.getPassenger(this.id).subscribe({
      next: journey => {
        this.journey.set(journey);
        this.form.controls.alternativeFlight.setValue(journey.alternativeFlights[0] ?? '');
      },
      error: () => {
        this.loadError.set('This passenger journey could not be loaded.');
        this.loading.set(false);
      },
      complete: () => this.loading.set(false),
    });
  }

  openRebook() {
    const journey = this.journey();
    if (!journey || journey.status === 'Rebooked') return;
    this.actionError.set('');
    this.success.set('');
    this.showRebook.set(true);
  }

  closeRebook() {
    if (this.submitting()) return;
    this.showRebook.set(false);
    this.actionError.set('');
  }

  rebook() {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.actionError.set('');
    const value = this.form.getRawValue();
    this.api.rebook(this.id, value.alternativeFlight, value.notes).subscribe({
      next: journey => {
        this.journey.set(journey);
        this.success.set(`${journey.partySize} traveler${journey.partySize === 1 ? '' : 's'} rebooked successfully.`);
        this.showRebook.set(false);
      },
      error: error => {
        this.actionError.set(error?.error?.message ??
          'The passenger journey could not be rebooked. Check the selected itinerary and try again.');
        this.submitting.set(false);
      },
      complete: () => this.submitting.set(false),
    });
  }
}
