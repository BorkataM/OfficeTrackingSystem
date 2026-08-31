import { Injectable, effect, signal } from '@angular/core';

export type ThemePreference = 'light' | 'dark' | 'system';

const STORAGE_KEY = 'office-days.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly systemPrefersDark = signal(matchDark().matches);

  readonly preference = signal<ThemePreference>(readStoredPreference());

  /** What is actually on screen, after resolving 'system'. */
  readonly resolved = signal<'light' | 'dark'>('light');

  constructor() {
    matchDark().addEventListener('change', (event) => this.systemPrefersDark.set(event.matches));

    effect(() => {
      const preference = this.preference();
      const resolved = preference === 'system' ? (this.systemPrefersDark() ? 'dark' : 'light') : preference;

      this.resolved.set(resolved);
      document.documentElement.dataset['theme'] = resolved;

      try {
        localStorage.setItem(STORAGE_KEY, preference);
      } catch {
        // A stored preference is a convenience, not a requirement.
      }
    });
  }

  toggle(): void {
    this.preference.set(this.resolved() === 'dark' ? 'light' : 'dark');
  }
}

function matchDark(): MediaQueryList {
  return window.matchMedia('(prefers-color-scheme: dark)');
}

function readStoredPreference(): ThemePreference {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);

    return stored === 'light' || stored === 'dark' || stored === 'system' ? stored : 'system';
  } catch {
    return 'system';
  }
}
