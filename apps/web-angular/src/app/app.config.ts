import { ApplicationConfig, ErrorHandler, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideStore } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { provideStoreDevtools } from '@ngrx/store-devtools';

import { routes } from './app.routes';
import { operationsInterceptor } from './core/interceptors/operations.interceptor';
import { flightsFeature } from './store/flights/flights.reducer';
import { FlightsEffects } from './store/flights/flights.effects';
import { GlobalErrorHandler } from './core/services/global-error-handler';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([operationsInterceptor])),
    provideStore({ [flightsFeature.name]: flightsFeature.reducer }),
    provideEffects(FlightsEffects),
    provideStoreDevtools({ maxAge: 25 }),
    { provide: ErrorHandler, useExisting: GlobalErrorHandler }
  ]
};
