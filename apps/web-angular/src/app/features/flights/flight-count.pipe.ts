import { Pipe, PipeTransform } from '@angular/core';
import { Flight } from '../../core/models/flight.model';
@Pipe({ name: 'flightCount', standalone: true })
export class FlightCountPipe implements PipeTransform {
  transform(flights: Flight[], mode: 'attention'|'passengers'|'risk'): number {
    if (!flights.length) return 0;
    if (mode === 'attention') return flights.filter(f =>
      f.status === 'At risk' || f.status === 'Delayed' || f.status === 'Cancelled').length;
    if (mode === 'passengers') return flights.reduce((sum, f) => sum + f.passengers, 0);
    return Math.round(flights.reduce((sum, f) => sum + f.risk, 0) / flights.length);
  }
}
