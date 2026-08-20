import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { Flight } from '../../core/models/flight.model';

export const FlightsActions = createActionGroup({
  source: 'Flights',
  events: {
    'Load': emptyProps(),
    'Load Success': props<{ flights: Flight[] }>(),
    'Load Failure': props<{ error: string }>(),
    'Select': props<{ id: string | null }>(),
    'Set Search': props<{ search: string }>(),
  }
});
