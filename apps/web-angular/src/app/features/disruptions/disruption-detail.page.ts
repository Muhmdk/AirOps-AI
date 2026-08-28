import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DisruptionEngineService } from '../../core/services/disruption-engine.service';
import { DisruptionApiService } from '../../core/services/disruption-api.service';
import { RecoveryApiService } from '../../core/services/recovery-api.service';
@Component({
  imports: [CommonModule, RouterLink],
  templateUrl: './disruption-detail.page.html',
  styleUrl: './disruption-detail.page.scss',
})
export class DisruptionDetailPage {
  readonly engine = inject(DisruptionEngineService);
  readonly api = inject(DisruptionApiService);
  readonly recoveryApi = inject(RecoveryApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly id = this.route.snapshot.paramMap.get('id') ?? '';
  readonly disruption = computed(
    () => this.engine.disruptions().find((d) => d.id === this.id) ?? null,
  );
  readonly audit = computed(() => this.engine.auditEntries().filter(entry => entry.disruptionId === this.id));
  readonly loading = signal(!this.engine.get(this.id));
  readonly resolving = signal(false);
  readonly generatingRecovery = signal(false);
  readonly actionError = signal('');
  constructor() {
    this.api.getDisruption(this.id).subscribe({
      error: () => this.loading.set(false),
      complete: () => this.loading.set(false),
    });
    this.api.getAudit(this.id).subscribe({ error: () => undefined });
  }
  resolve() {
    if (this.resolving()) return;
    this.actionError.set('');
    this.resolving.set(true);
    this.api.resolve(this.id).subscribe({
      next: () => this.router.navigate(['/disruptions']),
      error: () => {
        this.actionError.set('The disruption could not be resolved. Check the backend connection and try again.');
        this.resolving.set(false);
      },
    });
  }

  generateRecoveryPlans() {
    if (this.generatingRecovery()) return;
    this.actionError.set('');
    this.generatingRecovery.set(true);
    this.recoveryApi.generate(this.id).subscribe({
      next: () => this.router.navigate(['/recovery-plans', this.id]),
      error: () => {
        this.actionError.set('Recovery plans could not be generated. Check the backend connection and try again.');
        this.generatingRecovery.set(false);
      },
      complete: () => this.generatingRecovery.set(false),
    });
  }
}
