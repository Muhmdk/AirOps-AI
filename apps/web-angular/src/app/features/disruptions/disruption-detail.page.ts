import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DisruptionEngineService } from '../../core/services/disruption-engine.service';
@Component({
  imports: [CommonModule, RouterLink],
  templateUrl: './disruption-detail.page.html',
  styleUrl: './disruption-detail.page.scss',
})
export class DisruptionDetailPage {
  readonly engine = inject(DisruptionEngineService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly id = this.route.snapshot.paramMap.get('id') ?? '';
  readonly disruption = computed(
    () => this.engine.disruptions().find((d) => d.id === this.id) ?? null,
  );
  readonly audit = computed(() => this.engine.auditEntries().filter(entry => entry.disruptionId === this.id));
  resolve() {
    this.engine.resolve(this.id);
    this.router.navigate(['/disruptions']);
  }
}
