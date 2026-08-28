import { HttpClient } from '@angular/common/http';
import { Injectable, OnDestroy } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
} from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { OperationalEvent } from '../models/flight.model';

export type EventStreamStatus = 'connecting' | 'connected' | 'reconnecting' | 'offline';

const INITIAL_EVENTS: OperationalEvent[] = [
  { time: '09:08', type: 'risk', title: 'Weather risk raised', detail: 'Toronto Pearson · Severe thunderstorm cell', accent: 'amber', severity:'Critical', entityType:'airport', entityId:'YYZ', category:'Weather' },
  { time: '09:04', type: 'delay', title: 'AC418 delayed 42 min', detail: 'Late incoming aircraft · Gate D31', accent: 'red', severity:'Warning', entityType:'flight', entityId:'AC418', category:'Flight' },
  { time: '08:57', type: 'gate', title: 'Gate change · AC791', detail: 'A48 → A52 · Montréal Trudeau', accent: 'blue', severity:'Information', entityType:'flight', entityId:'AC791', category:'Gate' },
  { time: '08:51', type: 'ok', title: 'AC302 departed', detail: 'Toronto → Ottawa · 3 min early', accent: 'green', severity:'Information', entityType:'flight', entityId:'AC302', category:'Flight' },
  { time: '08:46', type: 'risk', title: 'Aircraft unavailable · C-GJYE', detail: 'Technical inspection required · Montréal', accent: 'red', severity:'Critical', entityType:'aircraft', entityId:'C-GJYE', category:'Aircraft' },
  { time: '08:39', type: 'risk', title: 'Passenger connections at risk', detail: 'AC103 · 47 protected connections', accent: 'amber', severity:'Warning', entityType:'flight', entityId:'AC103', category:'Passenger' },
];

@Injectable({ providedIn: 'root' })
export class OperationsEventService implements OnDestroy {
  private readonly eventState = new BehaviorSubject<OperationalEvent[]>(INITIAL_EVENTS);
  private readonly connectionState = new BehaviorSubject<EventStreamStatus>('offline');
  private connection?: HubConnection;
  private retryTimer?: ReturnType<typeof setTimeout>;
  private destroyed = false;
  readonly events$: Observable<OperationalEvent[]> = this.eventState.asObservable();
  readonly connectionStatus$: Observable<EventStreamStatus> = this.connectionState.asObservable();

  constructor(private readonly http?: HttpClient) {
    if (!http || typeof window === 'undefined') return;
    this.refreshHistory();
    void this.startHub();
  }

  publish(event: OperationalEvent) {
    this.prepend(event);
  }

  ngOnDestroy() {
    this.destroyed = true;
    if (this.retryTimer) clearTimeout(this.retryTimer);
    void this.connection?.stop();
  }

  private refreshHistory() {
    this.http?.get<OperationalEvent[]>('/api/operations/events?limit=50').subscribe({
      next: history => {
        const historyKeys = new Set(history.map(event => this.eventKey(event)));
        const localOnly = this.eventState.value.filter(
          event => !historyKeys.has(this.eventKey(event))
        );
        this.eventState.next([...history, ...localOnly].slice(0, 50));
      },
      error: () => undefined,
    });
  }

  private async startHub() {
    if (this.destroyed) return;
    if (!this.connection) {
      this.connectionState.next('connecting');
      this.connection = new HubConnectionBuilder()
        .withUrl(new URL('/hubs/operations', window.location.origin).toString())
        .withAutomaticReconnect([0, 2_000, 5_000, 10_000])
        .build();
      this.connection.on('operationalEvent', (event: OperationalEvent) => this.prepend(event));
      this.connection.onreconnecting(() => this.connectionState.next('reconnecting'));
      this.connection.onreconnected(() => {
        this.connectionState.next('connected');
        this.refreshHistory();
      });
      this.connection.onclose(() => {
        this.connectionState.next('offline');
        this.scheduleInitialRetry();
      });
    }

    if (this.connection.state !== HubConnectionState.Disconnected) return;

    try {
      await this.connection.start();
      this.connectionState.next('connected');
    } catch {
      this.connectionState.next('offline');
      this.scheduleInitialRetry();
    }
  }

  private scheduleInitialRetry() {
    if (this.destroyed) return;
    if (this.retryTimer) clearTimeout(this.retryTimer);
    this.retryTimer = setTimeout(() => {
      this.retryTimer = undefined;
      void this.startHub();
    }, 5_000);
  }

  private prepend(event: OperationalEvent) {
    const key = this.eventKey(event);
    const current = this.eventState.value.filter(item => this.eventKey(item) !== key);
    this.eventState.next([event, ...current].slice(0, 50));
  }

  private eventKey(event: OperationalEvent) {
    return `${event.time}|${event.title}|${event.detail}`;
  }
}
