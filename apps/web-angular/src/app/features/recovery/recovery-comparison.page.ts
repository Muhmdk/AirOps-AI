import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DisruptionEngineService } from '../../core/services/disruption-engine.service';
import { RecoveryEngineService } from '../../core/services/recovery-engine.service';
import { RecoveryPlan } from '../../core/models/recovery.model';
@Component({
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './recovery-comparison.page.html',
  styleUrl: './recovery-comparison.page.scss',
})
export class RecoveryComparisonPage {
  readonly disruptions = inject(DisruptionEngineService);
  readonly recovery = inject(RecoveryEngineService);
  private readonly route = inject(ActivatedRoute);
  readonly id = this.route.snapshot.paramMap.get('disruptionId') ?? '';
  readonly disruption = computed(() => this.disruptions.get(this.id));
  readonly plans = computed(() => this.recovery.forDisruption(this.id));
  readonly expanded = signal('');
  readonly selected = signal<RecoveryPlan | null>(null);
  readonly decisionMode = signal<'approve' | 'reject'>('approve');
  readonly notes = signal('');
  readonly supervisorOverride = signal(false);
  readonly approvalError = signal('');
  readonly approvedOutcome = computed(() =>
    this.recovery.auditEntries().find(
      (entry) => entry.disruptionId === this.id && entry.action === 'Approved',
    ),
  );
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
    if (!plan) return;
    const result = this.recovery.approve(
      plan.id,
      this.notes(),
      this.supervisorOverride(),
    );
    this.approvalError.set(result.error);
    if (result.ok) this.closeDecision();
  }
  reject() {
    const plan = this.selected();
    if (!plan) return;
    if (!this.notes().trim()) {
      this.approvalError.set('Add a decision note before rejecting this plan');
      return;
    }
    if (this.recovery.reject(plan.id, this.notes().trim())) this.closeDecision();
  }
}
