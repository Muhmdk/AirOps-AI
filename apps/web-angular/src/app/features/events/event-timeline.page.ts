import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnDestroy, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { OperationalEvent } from '../../core/models/flight.model';
import {
  EventStreamStatus,
  OperationsEventService,
} from '../../core/services/operations-event.service';

@Component({
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './event-timeline.page.html',
  styleUrl: './event-timeline.page.scss',
})
export class EventTimelinePage implements OnDestroy {
  private readonly service = inject(OperationsEventService);
  private readonly router = inject(Router);
  private readonly subscriptions = new Subscription();
  readonly events = signal<OperationalEvent[]>([]);
  readonly paused = signal(false);
  readonly pending = signal(0);
  readonly search = signal('');
  readonly severity = signal('All');
  readonly category = signal('All');
  readonly latest = signal<OperationalEvent[]>([]);
  readonly connectionStatus = signal<EventStreamStatus>('offline');
  readonly filtered = computed(() => {
    const query = this.search().trim().toLowerCase();
    return this.events().filter(event =>
      (!query || `${event.title} ${event.detail} ${event.entityId}`.toLowerCase().includes(query)) &&
      (this.severity() === 'All' || event.severity === this.severity()) &&
      (this.category() === 'All' || event.category === this.category())
    );
  });
  readonly counts = computed(() => ({
    critical: this.events().filter(event => event.severity === 'Critical').length,
    warning: this.events().filter(event => event.severity === 'Warning').length,
    info: this.events().filter(event => event.severity === 'Information').length,
  }));

  constructor() {
    this.subscriptions.add(this.service.events$.subscribe(events => {
      this.latest.set(events);
      if (this.paused()) {
        if (events[0]?.id !== this.events()[0]?.id || events[0]?.title !== this.events()[0]?.title)
          this.pending.update(count => count + 1);
      } else {
        this.events.set(events);
      }
    }));
    this.subscriptions.add(this.service.connectionStatus$.subscribe(
      status => this.connectionStatus.set(status)
    ));
  }

  toggle() {
    if (this.paused()) {
      this.paused.set(false);
      this.events.set(this.latest());
      this.pending.set(0);
    } else {
      this.paused.set(true);
    }
  }

  open(event: OperationalEvent) {
    if (!event.entityType || !event.entityId) return;
    const prefix = event.entityType === 'flight'
      ? 'flights'
      : event.entityType === 'airport' ? 'airports' : 'aircraft';
    this.router.navigate(['/', prefix, event.entityId]);
  }

  clear() {
    this.search.set('');
    this.severity.set('All');
    this.category.set('All');
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
  }
}
