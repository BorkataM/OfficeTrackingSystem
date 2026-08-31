import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthApi } from '../api/auth.api';
import { AuthService } from './auth.service';
import { SessionStore } from './session.store';
import { TokenRefreshCoordinator } from './token-refresh.coordinator';

/** Endpoints that must never carry a bearer token or trigger a refresh. */
const ANONYMOUS_PATHS = ['/auth/login', '/auth/register', '/auth/refresh'];

/**
 * Attaches the access token and, on a 401, refreshes once and replays the request.
 * Concurrent 401s share a single refresh through the coordinator, so a page that
 * fires several requests at load does not burn several refresh tokens.
 */
export function authInterceptor(
  request: HttpRequest<unknown>,
  next: HttpHandlerFn,
): Observable<HttpEvent<unknown>> {
  const session = inject(SessionStore);
  const authApi = inject(AuthApi);
  const auth = inject(AuthService);
  const coordinator = inject(TokenRefreshCoordinator);

  if (ANONYMOUS_PATHS.some((path) => request.url.includes(path))) {
    return next(request);
  }

  return next(withBearer(request, session.accessToken)).pipe(
    catchError((error: unknown) => {
      if (!isUnauthorized(error) || !session.refreshToken) {
        return throwError(() => error);
      }

      if (coordinator.isRefreshing) {
        return coordinator.settled$.pipe(
          filter((token): token is string => token !== null),
          take(1),
          switchMap((token) => next(withBearer(request, token))),
        );
      }

      const refreshToken = session.refreshToken;
      coordinator.begin();

      return authApi.refresh(refreshToken).pipe(
        switchMap((result) => {
          session.renew(result);
          coordinator.succeed(result.accessToken);

          return next(withBearer(request, result.accessToken));
        }),
        catchError((refreshError: unknown) => {
          coordinator.fail();
          auth.endSessionAndRedirect();

          return throwError(() => refreshError);
        }),
      );
    }),
  );
}

function withBearer(request: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
}

function isUnauthorized(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === 401;
}
