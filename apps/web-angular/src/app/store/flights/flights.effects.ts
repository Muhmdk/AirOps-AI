import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, of, switchMap } from 'rxjs';
import { FlightApiService } from '../../core/services/flight-api.service';
import { FlightsActions } from './flights.actions';

@Injectable()
export class FlightsEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(FlightApiService);
  readonly load$ = createEffect(() => this.actions$.pipe(
    ofType(FlightsActions.load),
    switchMap(() => this.api.getFlights().pipe(
      map(flights => FlightsActions.loadSuccess({ flights })),
      catchError(error => of(FlightsActions.loadFailure({ error: error instanceof Error ? error.message : 'Unable to load flights' })))
    ))
  ));
}
