import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Disruption } from '../../core/models/disruption.model';
import { DisruptionApiService } from '../../core/services/disruption-api.service';
import { DisruptionEngineService } from '../../core/services/disruption-engine.service';
import { RecoveryApiService } from '../../core/services/recovery-api.service';
import { RecoveryEngineService } from '../../core/services/recovery-engine.service';

@Component({
  imports: [CommonModule, RouterLink],
  templateUrl: './recovery-plans.page.html',
  styleUrl: './recovery-plans.page.scss',
})
export class RecoveryPlansPage {
  readonly disruptions = inject(DisruptionEngineService);
  readonly recovery = inject(RecoveryEngineService);
  readonly api = inject(RecoveryApiService);
  private readonly disruptionApi = inject(DisruptionApiService);
  private readonly router = inject(Router);
  readonly generating = signal('');

  constructor() {
    this.disruptionApi.getDisruptions().subscribe({ error: () => undefined });
    this.api.getDecisionLog().subscribe({ error: () => undefined });
  }

  open(disruption: Disruption) {
    if (this.generating()) return;
    this.generating.set(disruption.id);
    this.api.generate(disruption.id).subscribe({
      next: () => this.router.navigate(['/recovery-plans', disruption.id]),
      error: () => this.generating.set(''),
      complete: () => this.generating.set(''),
    });
  }
}
