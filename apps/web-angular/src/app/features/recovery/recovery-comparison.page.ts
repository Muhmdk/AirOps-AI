import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DisruptionEngineService } from '../../core/services/disruption-engine.service';
import { RecoveryEngineService } from '../../core/services/recovery-engine.service';
import { RecoveryPlan } from '../../core/models/recovery.model';
import { RecoveryApiService } from '../../core/services/recovery-api.service';
import { DisruptionApiService } from '../../core/services/disruption-api.service';
@Component({
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './recovery-comparison.page.html',
  styleUrl: './recovery-comparison.page.scss',
})
export class RecoveryComparisonPage {
  readonly disruptions = inject(DisruptionEngineService);
  readonly recovery = inject(RecoveryEngineService);
  readonly api = inject(RecoveryApiService);
  private readonly disruptionApi = inject(DisruptionApiService);
  private readonly route = inject(ActivatedRoute);
  readonly id = this.route.snapshot.paramMap.get('disruptionId') ?? '';
  readonly disruption = computed(() => this.disruptions.get(this.id));
  readonly plans = computed(() => this.recovery.plans()[this.id] ?? []);
  readonly expanded = signal('');
  readonly selected = signal<RecoveryPlan | null>(null);
  readonly decisionMode = signal<'approve' | 'reject'>('approve');
  readonly notes = signal('');
  readonly supervisorOverride = signal(false);
  readonly approvalError = signal('');
  readonly submitting = signal(false);
  readonly approvedOutcome = computed(() =>
    this.recovery.auditEntries().find(
      (entry) => entry.disruptionId === this.id && entry.action === 'Approved',
    ),
  );
  constructor() {
    this.disruptionApi.getDisruption(this.id).subscribe({ error: () => undefined });
    this.api.generate(this.id).subscribe({ error: () => undefined });
    this.api.getDecisionLog().subscribe({ error: () => undefined });
  }
  openDecision(plan: RecoveryPlan, mode: 'approve' | 'reject') {
    this.selected.set(plan);
    this.decisionMode.set(mode);
    this.notes.set('');
    this.supervisorOverride.set(false);
    this.approvalError.set('');
  }
  closeDecision() {
    this.selected.set(null);
    this.approvalError.set('');
  }
  approve() {
    const plan = this.selected();
    if (!plan || this.submitting()) return;
    if (!this.notes().trim()) {
      this.approvalError.set('Add a decision note before approving this plan');
      return;
    }
    this.submitting.set(true);
    this.api.approve(plan, this.notes().trim(), this.supervisorOverride()).subscribe({
      next: () => this.closeDecision(),
      error: error => {
        this.approvalError.set(error instanceof Error ? error.message : 'Approval failed');
        this.submitting.set(false);
      },
      complete: () => this.submitting.set(false),
    });
  }
  reject() {
    const plan = this.selected();
    if (!plan || this.submitting()) return;
    if (!this.notes().trim()) {
      this.approvalError.set('Add a decision note before rejecting this plan');
      return;
    }
    this.submitting.set(true);
    this.api.reject(plan, this.notes().trim()).subscribe({
      next: () => this.closeDecision(),
      error: error => {
        this.approvalError.set(error instanceof Error ? error.message : 'Rejection failed');
        this.submitting.set(false);
      },
      complete: () => this.submitting.set(false),
    });
  }
}
