import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TeamDetail, TeamListItem } from '../api/api.models';
import { TeamsApi } from '../api/teams.api';

const SELECTED_TEAM_KEY = 'office-days.selectedTeam';

/**
 * The user's team memberships and which one they are currently looking at. The
 * selection is remembered across reloads, because coming back to a different
 * team than you left is disorienting.
 */
@Injectable({ providedIn: 'root' })
export class TeamStore {
  private readonly api = inject(TeamsApi);

  private readonly teamsSignal = signal<readonly TeamListItem[]>([]);
  private readonly selectedIdSignal = signal<string | null>(readStoredSelection());
  private readonly loadingSignal = signal(false);
  private readonly loadedSignal = signal(false);

  readonly teams = this.teamsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly loaded = this.loadedSignal.asReadonly();
  readonly selectedId = this.selectedIdSignal.asReadonly();

  readonly selected = computed(() => {
    const id = this.selectedIdSignal();

    return this.teamsSignal().find((team) => team.id === id) ?? null;
  });

  readonly hasTeams = computed(() => this.teamsSignal().length > 0);

  async load(force = false): Promise<readonly TeamListItem[]> {
    if (this.loadedSignal() && !force) {
      return this.teamsSignal();
    }

    this.loadingSignal.set(true);

    try {
      const teams = await firstValueFrom(this.api.list());
      this.teamsSignal.set(teams);
      this.loadedSignal.set(true);
      this.reconcileSelection(teams);

      return teams;
    } finally {
      this.loadingSignal.set(false);
    }
  }

  select(teamId: string): void {
    this.selectedIdSignal.set(teamId);
    safeWrite(teamId);
  }

  /** Called after create/join so the switcher shows the new team immediately. */
  adopt(team: TeamDetail): void {
    const item: TeamListItem = {
      id: team.id,
      name: team.name,
      description: team.description,
      myRole: team.myRole,
      memberCount: team.members.length,
      createdAtUtc: team.createdAtUtc,
    };

    this.teamsSignal.update((current) => {
      const without = current.filter((existing) => existing.id !== team.id);

      return [...without, item].sort((a, b) => a.name.localeCompare(b.name));
    });

    this.loadedSignal.set(true);
    this.select(team.id);
  }

  forget(teamId: string): void {
    this.teamsSignal.update((current) => current.filter((team) => team.id !== teamId));

    if (this.selectedIdSignal() === teamId) {
      this.selectedIdSignal.set(null);
      safeRemove();
      this.reconcileSelection(this.teamsSignal());
    }
  }

  reset(): void {
    this.teamsSignal.set([]);
    this.loadedSignal.set(false);
    this.selectedIdSignal.set(null);
    safeRemove();
  }

  /** Keeps the remembered selection honest: a team you left must not stay selected. */
  private reconcileSelection(teams: readonly TeamListItem[]): void {
    const current = this.selectedIdSignal();

    if (current && teams.some((team) => team.id === current)) {
      return;
    }

    const fallback = teams[0]?.id ?? null;
    this.selectedIdSignal.set(fallback);

    if (fallback) {
      safeWrite(fallback);
    } else {
      safeRemove();
    }
  }
}

function readStoredSelection(): string | null {
  try {
    return localStorage.getItem(SELECTED_TEAM_KEY);
  } catch {
    return null;
  }
}

function safeWrite(teamId: string): void {
  try {
    localStorage.setItem(SELECTED_TEAM_KEY, teamId);
  } catch {
    // Remembering the selection is a nicety, not a requirement.
  }
}

function safeRemove(): void {
  try {
    localStorage.removeItem(SELECTED_TEAM_KEY);
  } catch {
    // Nothing to do.
  }
}
