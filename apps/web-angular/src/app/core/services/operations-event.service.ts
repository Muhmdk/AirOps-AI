import { Injectable } from '@angular/core';
import { interval, map, merge, Observable, of, ReplaySubject, scan, shareReplay } from 'rxjs';
import { OperationalEvent } from '../models/flight.model';

const INITIAL_EVENTS: OperationalEvent[] = [
  { time: '09:08', type: 'risk', title: 'Weather risk raised', detail: 'Toronto Pearson · Severe thunderstorm cell', accent: 'amber', severity:'Critical', entityType:'airport', entityId:'YYZ', category:'Weather' },
  { time: '09:04', type: 'delay', title: 'AC418 delayed 42 min', detail: 'Late incoming aircraft · Gate D31', accent: 'red', severity:'Warning', entityType:'flight', entityId:'AC418', category:'Flight' },
  { time: '08:57', type: 'gate', title: 'Gate change · AC791', detail: 'A48 → A52 · Montréal Trudeau', accent: 'blue', severity:'Information', entityType:'flight', entityId:'AC791', category:'Gate' },
  { time: '08:51', type: 'ok', title: 'AC302 departed', detail: 'Toronto → Ottawa · 3 min early', accent: 'green', severity:'Information', entityType:'flight', entityId:'AC302', category:'Flight' },
  { time: '08:46', type: 'risk', title: 'Aircraft unavailable · C-GJYE', detail: 'Technical inspection required · Montréal', accent: 'red', severity:'Critical', entityType:'aircraft', entityId:'C-GJYE', category:'Aircraft' },
  { time: '08:39', type: 'risk', title: 'Passenger connections at risk', detail: 'AC103 · 47 protected connections', accent: 'amber', severity:'Warning', entityType:'flight', entityId:'AC103', category:'Passenger' },
];

@Injectable({ providedIn: 'root' })
export class OperationsEventService {
  private readonly published = new ReplaySubject<OperationalEvent[]>(20);
  readonly events$: Observable<OperationalEvent[]> = merge(
    of(INITIAL_EVENTS),
    interval(15000).pipe(map(index => [{
      time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      type: index % 2 ? 'ok' : 'gate', title: index % 2 ? 'Turnaround completed · AC621' : 'Gate assignment updated · AC224',
      detail: index % 2 ? 'Vancouver · Ready for boarding' : 'Toronto Pearson · Gate D18', accent: index % 2 ? 'green' : 'blue',
      severity: 'Information', entityType: index % 2 ? 'flight' : 'airport', entityId: index % 2 ? 'AC621' : 'YYZ', category: index % 2 ? 'Flight' : 'Gate'
    } satisfies OperationalEvent])),
    this.published
  ).pipe(
    scan((events, next) => [...next, ...events].slice(0, 6), [] as OperationalEvent[]),
    shareReplay({ bufferSize: 1, refCount: true })
  );
  publish(event: OperationalEvent) { this.published.next([event]); }
}
