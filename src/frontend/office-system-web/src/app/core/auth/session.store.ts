import { Injectable, computed, signal } from '@angular/core';
import { AuthenticatedUser, AuthenticationResult } from '../api/api.models';

const STORAGE_KEY = 'office-days.session';

interface PersistedSession {
  readonly accessToken: string;
  readonly refreshToken: string;
  readonly user: AuthenticatedUser;
}

/**
 * Holds the signed-in session and mirrors it into localStorage so a refresh of the
 * page does not sign the user out. Every read is a signal, so components re-render
 * on sign-in and sign-out without any manual subscription.
 */
@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly session = signal<PersistedSession | null>(readStoredSession());

  readonly user = computed(() => this.session()?.user ?? null);
  readonly isAuthenticated = computed(() => this.session() !== null);

  get accessToken(): string | null {
    return this.session()?.accessToken ?? null;
  }

  get refreshToken(): string | null {
    return this.session()?.refreshToken ?? null;
  }

  start(result: AuthenticationResult): void {
    this.write({
      accessToken: result.accessToken,
      refreshToken: result.refreshToken,
      user: result.user,
    });
  }

  /** Applied after a silent refresh: new tokens, same identity. */
  renew(result: AuthenticationResult): void {
    this.write({
      accessToken: result.accessToken,
      refreshToken: result.refreshToken,
      user: result.user,
    });
  }

  updateUser(user: AuthenticatedUser): void {
    const current = this.session();

    if (current) {
      this.write({ ...current, user });
    }
  }

  clear(): void {
    this.session.set(null);
    safeRemove();
  }

  private write(next: PersistedSession): void {
    this.session.set(next);
    safeWrite(next);
  }
}

function readStoredSession(): PersistedSession | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);

    if (!raw) {
      return null;
    }

    const parsed = JSON.parse(raw) as Partial<PersistedSession>;

    // A partially-written or outdated entry is discarded rather than trusted.
    if (!parsed.accessToken || !parsed.refreshToken || !parsed.user?.id) {
      return null;
    }

    return parsed as PersistedSession;
  } catch {
    return null;
  }
}

function safeWrite(session: PersistedSession): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  } catch {
    // Private browsing or a full quota: the session simply will not survive a reload.
  }
}

function safeRemove(): void {
  try {
    localStorage.removeItem(STORAGE_KEY);
  } catch {
    // Nothing to do; the in-memory session is already cleared.
  }
}
