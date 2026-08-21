import { HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
export const operationsInterceptor: HttpInterceptorFn = (request, next) => {
  const requestId = crypto.randomUUID();
  const startedAt = performance.now();
  return next(request.clone({ setHeaders: { 'X-Correlation-Id': requestId } })).pipe(catchError(error => {
    console.error('[AirOps API]', { requestId, url: request.url, durationMs: Math.round(performance.now() - startedAt), error });
    return throwError(() => error);
  }));
};
