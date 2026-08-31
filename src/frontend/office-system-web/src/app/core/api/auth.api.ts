import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api.config';
import { AuthenticatedUser, AuthenticationResult } from './api.models';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/auth`;

  register(email: string, displayName: string, password: string): Observable<AuthenticationResult> {
    return this.http.post<AuthenticationResult>(`${this.baseUrl}/register`, { email, displayName, password });
  }

  login(email: string, password: string): Observable<AuthenticationResult> {
    return this.http.post<AuthenticationResult>(`${this.baseUrl}/login`, { email, password });
  }

  refresh(refreshToken: string): Observable<AuthenticationResult> {
    return this.http.post<AuthenticationResult>(`${this.baseUrl}/refresh`, { refreshToken });
  }

  signOut(refreshToken: string | null): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/sign-out`, { refreshToken });
  }

  me(): Observable<AuthenticatedUser> {
    return this.http.get<AuthenticatedUser>(`${this.baseUrl}/me`);
  }

  updateDisplayName(displayName: string): Observable<AuthenticatedUser> {
    return this.http.put<AuthenticatedUser>(`${this.baseUrl}/me`, { displayName });
  }
}
