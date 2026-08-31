import { InjectionToken } from '@angular/core';

/**
 * Injected rather than imported from an environment file, so a test or a
 * differently-hosted build can point the client somewhere else without a rebuild
 * of the services themselves.
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '/api',
});
