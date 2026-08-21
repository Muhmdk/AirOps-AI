import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { createFeature, createReducer, createSelector, on } from '@ngrx/store';
import { Flight } from '../../core/models/flight.model';
import { FlightsActions } from './flights.actions';

export interface FlightsState extends EntityState<Flight> { loading: boolean; error: string | null; selectedId: string | null; search: string; }
const adapter = createEntityAdapter<Flight>();
const initialState: FlightsState = adapter.getInitialState({ loading: false, error: null, selectedId: null, search: '' });

export const flightsFeature = createFeature({
  name: 'flights',
  reducer: createReducer(
    initialState,
    on(FlightsActions.load, state => ({ ...state, loading: true, error: null })),
    on(FlightsActions.loadSuccess, (state, { flights }) => adapter.setAll(flights, { ...state, loading: false })),
    on(FlightsActions.loadFailure, (state, { error }) => ({ ...state, loading: false, error })),
    on(FlightsActions.select, (state, { id }) => ({ ...state, selectedId: id })),
    on(FlightsActions.setSearch, (state, { search }) => ({ ...state, search })),
  ),
  extraSelectors: ({ selectFlightsState, selectSelectedId, selectSearch }) => {
    const selectors = adapter.getSelectors(selectFlightsState);
    return {
      ...selectors,
      selectSelectedFlight: createSelector(selectors.selectEntities, selectSelectedId, (entities, id) => entities[id ?? ''] ?? null),
      selectFilteredFlights: createSelector(selectors.selectAll, selectSearch, (flights, search) => {
        const q = search.trim().toLowerCase();
        return q ? flights.filter(f => `${f.id} ${f.route} ${f.origin} ${f.destination}`.toLowerCase().includes(q)) : flights;
      })
    };
  }
});
