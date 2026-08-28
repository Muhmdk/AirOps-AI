import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of, tap, throwError } from 'rxjs';
import {
  Disruption,
  DisruptionAuditEntry,
  DisruptionScenario,
} from '../models/disruption.model';
import { DisruptionEngineService } from './disruption-engine.service';

interface DisruptionApiResponse extends Omit<Disruption, 'startedAt'> {
  startedAt: string;
  resolvedAt?: string | null;
}

@Injectable({ providedIn: 'root' })
export class DisruptionApiService {
  readonly source = signal<'loading' | 'backend' | 'fallback'>('fallback');
  readonly connectionError = signal<string | null>(null);

  constructor(
    private readonly engine: DisruptionEngineService,
    private readonly http?: HttpClient,
  ) {}

  getDisruptions(): Observable<Disruption[]> {
    if (!this.http) return of(this.engine.disruptions());
    this.source.set('loading');
    this.connectionError.set(null);
    return this.http.get<DisruptionApiResponse[]>('/api/disruptions').pipe(
      map(items => items.map(item => this.toDisruption(item))),
      tap(items => {
        this.engine.hydrateDisruptions(items);
        this.source.set('backend');
      }),
      catchError(() => this.offline(this.engine.disruptions()))
    );
  }

  getDisruption(id: string): Observable<Disruption> {
    const local = this.engine.get(id);
    if (!this.http) return local
      ? of(local)
      : throwError(() => new Error(`Disruption '${id}' was not found.`));

    return this.http.get<DisruptionApiResponse>(
      `/api/disruptions/${encodeURIComponent(id)}`
    ).pipe(
      map(item => this.toDisruption(item)),
      tap(item => {
        this.engine.upsertDisruption(item);
        this.source.set('backend');
      }),
      catchError(error => local && this.canFallback(error)
        ? this.offline(local)
        : throwError(() => error))
    );
  }

  getAudit(id: string): Observable<DisruptionAuditEntry[]> {
    const local = this.engine.auditEntries().filter(entry => entry.disruptionId === id);
    if (!this.http) return of(local);
    return this.http.get<DisruptionAuditEntry[]>(
      `/api/disruptions/${encodeURIComponent(id)}/audit`
    ).pipe(
      tap(entries => this.engine.hydrateAudit(id, entries)),
      catchError(error => this.canFallback(error)
        ? this.offline(local)
        : throwError(() => error))
    );
  }

  create(scenario: DisruptionScenario): Observable<Disruption> {
    if (!this.http) return of(this.engine.create(scenario));
    this.connectionError.set(null);
    return this.http.post<DisruptionApiResponse>('/api/disruptions', {
      type: scenario.type,
      severity: scenario.severity,
      airport: scenario.airport,
      flightId: scenario.flightId,
      durationMinutes: scenario.durationMinutes,
    }).pipe(
      map(item => this.toDisruption(item)),
      tap(item => {
        this.engine.upsertDisruption(item);
        this.source.set('backend');
      }),
      catchError(error => this.isOffline(error)
        ? this.offline(this.engine.create(scenario))
        : throwError(() => error))
    );
  }

  resolve(id: string): Observable<Disruption> {
    const local = this.engine.get(id);
    if (!this.http) {
      this.engine.resolve(id);
      return this.engine.get(id)
        ? of(this.engine.get(id)!)
        : throwError(() => new Error(`Disruption '${id}' was not found.`));
    }

    return this.http.post<DisruptionApiResponse>(
      `/api/disruptions/${encodeURIComponent(id)}/resolve`, null
    ).pipe(
      map(item => this.toDisruption(item)),
      tap(item => {
        this.engine.upsertDisruption(item);
        this.source.set('backend');
      }),
      catchError(error => {
        if (!local || !this.canFallback(error)) return throwError(() => error);
        this.engine.resolve(id);
        return this.offline(this.engine.get(id)!);
      })
    );
  }

  private canFallback(error: unknown) {
    return this.isOffline(error) ||
      (error instanceof HttpErrorResponse && error.status === 404);
  }

  private isOffline(error: unknown) {
    return !(error instanceof HttpErrorResponse) || error.status === 0 || error.status >= 500;
  }

  private offline<T>(value: T): Observable<T> {
    this.source.set('fallback');
    this.connectionError.set('Backend unavailable; using the browser disruption engine.');
    return of(value);
  }

  private toDisruption(response: DisruptionApiResponse): Disruption {
    return {
      ...response,
      startedAt: response.startedAt.match(/T(\d{2}:\d{2})/)?.[1] ?? response.startedAt,
    };
  }
}
