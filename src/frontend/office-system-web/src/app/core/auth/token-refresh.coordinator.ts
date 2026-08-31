import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';

/**
 * Serialises token refreshes. Without this, N requests that all 401 at once would
 * each present the same refresh token — and since the API rotates on every use,
 * only the first would succeed.
 */
@Injectable({ providedIn: 'root' })
export class TokenRefreshCoordinator {
  private refreshing = false;
  private readonly settled = new Subject<string | null>();

  get isRefreshing(): boolean {
    return this.refreshing;
  }

  /** Emits the new access token once a refresh finishes, or null if it failed. */
  get settled$(): Observable<string | null> {
    return this.settled.asObservable();
  }

  begin(): void {
    this.refreshing = true;
  }

  succeed(accessToken: string): void {
    this.refreshing = false;
    this.settled.next(accessToken);
  }

  fail(): void {
    this.refreshing = false;
    this.settled.next(null);
  }
}
