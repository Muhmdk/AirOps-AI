import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of, switchMap, tap, throwError } from 'rxjs';
import { RecoveryAuditEntry, RecoveryPlan } from '../models/recovery.model';
import { RecoveryEngineService } from './recovery-engine.service';

interface RecoveryDecisionResponse {
  plan: RecoveryPlan;
  audit: RecoveryAuditEntry;
}

@Injectable({ providedIn: 'root' })
export class RecoveryApiService {
  readonly source = signal<'loading' | 'backend' | 'fallback'>('fallback');
  readonly connectionError = signal<string | null>(null);

  constructor(
    private readonly engine: RecoveryEngineService,
    private readonly http?: HttpClient,
  ) {}

  getPlans(disruptionId: string): Observable<RecoveryPlan[]> {
    if (!this.http) return of(this.engine.plans()[disruptionId] ?? []);
    return this.http.get<RecoveryPlan[]>(
      `/api/disruptions/${encodeURIComponent(disruptionId)}/recovery-plans`
    ).pipe(
      tap(plans => {
        this.engine.hydratePlans(disruptionId, plans);
        this.source.set('backend');
      }),
      catchError(error => this.isOffline(error)
        ? this.offline(this.engine.plans()[disruptionId] ?? [])
        : throwError(() => error))
    );
  }

  generate(disruptionId: string): Observable<RecoveryPlan[]> {
    if (!this.http) return of(this.engine.forDisruption(disruptionId));
    this.source.set('loading');
    return this.http.post<RecoveryPlan[]>(
      `/api/disruptions/${encodeURIComponent(disruptionId)}/recovery-plans/generate`, null
    ).pipe(
      tap(plans => {
        this.engine.hydratePlans(disruptionId, plans);
        this.source.set('backend');
      }),
      catchError(error => this.isOffline(error)
        ? this.offline(this.engine.forDisruption(disruptionId))
        : throwError(() => error))
    );
  }

  getDecisionLog(): Observable<RecoveryAuditEntry[]> {
    if (!this.http) return of(this.engine.auditEntries());
    return this.http.get<RecoveryAuditEntry[]>('/api/recovery-decisions').pipe(
      tap(entries => this.engine.hydrateAudit(entries)),
      catchError(error => this.isOffline(error)
        ? this.offline(this.engine.auditEntries())
        : throwError(() => error))
    );
  }

  approve(plan: RecoveryPlan, notes: string, supervisorOverride: boolean) {
    if (!this.http) return this.approveOffline(plan, notes, supervisorOverride);
    return this.decide(plan, 'approve', notes, supervisorOverride);
  }

  reject(plan: RecoveryPlan, notes: string) {
    if (!this.http) return this.rejectOffline(plan, notes);
    return this.decide(plan, 'reject', notes, false);
  }

  private decide(
    plan: RecoveryPlan,
    action: 'approve' | 'reject',
    notes: string,
    supervisorOverride: boolean,
  ): Observable<RecoveryDecisionResponse> {
    return this.http!.post<RecoveryDecisionResponse>(
      `/api/recovery-plans/${encodeURIComponent(plan.id)}/${action}`,
      { notes, supervisorOverride }
    ).pipe(
      catchError(error => this.isOffline(error)
        ? action === 'approve'
          ? this.approveOffline(plan, notes, supervisorOverride)
          : this.rejectOffline(plan, notes)
        : throwError(() => error)),
      tap(decision => {
        this.engine.upsertPlan(decision.plan);
        this.engine.prependAudit(decision.audit);
      }),
      switchMap(decision => this.getPlans(plan.disruptionId).pipe(map(() => decision)))
    );
  }

  private approveOffline(plan: RecoveryPlan, notes: string, supervisorOverride: boolean) {
    const result = this.engine.approve(plan.id, notes, supervisorOverride);
    if (!result.ok) return throwError(() => new Error(result.error));
    return this.offline({
      plan: this.engine.getPlan(plan.id)!,
      audit: this.engine.auditEntries()[0],
    });
  }

  private rejectOffline(plan: RecoveryPlan, notes: string) {
    if (!this.engine.reject(plan.id, notes))
      return throwError(() => new Error('Recovery plan not found'));
    return this.offline({
      plan: this.engine.getPlan(plan.id)!,
      audit: this.engine.auditEntries()[0],
    });
  }

  private isOffline(error: unknown) {
    return !(error instanceof HttpErrorResponse) || error.status === 0 || error.status >= 500;
  }

  private offline<T>(value: T): Observable<T> {
    this.source.set('fallback');
    this.connectionError.set('Backend unavailable; using the browser recovery engine.');
    return of(value);
  }
}
