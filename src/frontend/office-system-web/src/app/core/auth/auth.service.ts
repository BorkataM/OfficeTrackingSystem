import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthenticatedUser, AuthenticationResult } from '../api/api.models';
import { AuthApi } from '../api/auth.api';
import { SessionStore } from './session.store';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(AuthApi);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);

  readonly user = this.session.user;
  readonly isAuthenticated = this.session.isAuthenticated;

  register(email: string, displayName: string, password: string): Observable<AuthenticationResult> {
    return this.api.register(email, displayName, password).pipe(tap((result) => this.session.start(result)));
  }

  login(email: string, password: string): Observable<AuthenticationResult> {
    return this.api.login(email, password).pipe(tap((result) => this.session.start(result)));
  }

  updateDisplayName(displayName: string): Observable<AuthenticatedUser> {
    return this.api.updateDisplayName(displayName).pipe(tap((user) => this.session.updateUser(user)));
  }

  /**
   * Clears the local session first, then tells the server. The order matters: this
   * tab is signed out even if the network call fails.
   */
  signOut(): void {
    const refreshToken = this.session.refreshToken;

    this.session.clear();

    if (refreshToken) {
      this.api.signOut(refreshToken).subscribe({ error: () => undefined });
    }

    void this.router.navigate(['/sign-in']);
  }

  /** Called by the interceptor once a refresh attempt has failed for good. */
  endSessionAndRedirect(): void {
    const returnUrl = this.router.url;

    this.session.clear();

    void this.router.navigate(['/sign-in'], {
      queryParams: returnUrl.startsWith('/sign-in') ? {} : { returnUrl },
    });
  }
}
