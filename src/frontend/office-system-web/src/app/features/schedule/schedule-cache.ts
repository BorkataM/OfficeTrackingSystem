import { Injectable } from '@angular/core';
import { IsoDate, TeamSchedule } from '../../core/api/api.models';
import { PlanChange, withPlanChanges } from './plan-mutations';

/** Bounded so a long session cannot grow this without limit. */
const MAX_ENTRIES = 24;

/**
 * Weeks already fetched, kept for the session.
 *
 * Week navigation used to cost a round trip and a blank grid every time. With a
 * cache the grid paints from memory and the request becomes a background
 * revalidation, so stepping back and forth is instant.
 */
@Injectable({ providedIn: 'root' })
export class ScheduleCache {
  /** Insertion order is the eviction order, so this doubles as an LRU list. */
  private readonly entries = new Map<string, TeamSchedule>();

  get(teamId: string, weekStart: IsoDate): TeamSchedule | null {
    const key = cacheKey(teamId, weekStart);
    const hit = this.entries.get(key);

    if (hit === undefined) {
      return null;
    }

    this.entries.delete(key);
    this.entries.set(key, hit);

    return hit;
  }

  has(teamId: string, weekStart: IsoDate): boolean {
    return this.entries.has(cacheKey(teamId, weekStart));
  }

  set(teamId: string, schedule: TeamSchedule): void {
    const key = cacheKey(teamId, schedule.start);

    this.entries.delete(key);
    this.entries.set(key, schedule);

    while (this.entries.size > MAX_ENTRIES) {
      const oldest = this.entries.keys().next();

      if (oldest.done) {
        break;
      }

      this.entries.delete(oldest.value);
    }
  }

  /**
   * Applies an optimistic edit to every cached week it touches, so navigating away
   * and back does not resurrect the pre-edit copy.
   */
  patch(teamId: string, userId: string, changes: readonly PlanChange[]): void {
    const prefix = `${teamId}|`;

    for (const [key, schedule] of this.entries) {
      if (!key.startsWith(prefix)) {
        continue;
      }

      const covered = changes.filter((change) => schedule.days.includes(change.date));

      if (covered.length > 0) {
        this.entries.set(key, withPlanChanges(schedule, userId, covered));
      }
    }
  }

  /** Dropped when the server disagrees with us, so the next read is authoritative. */
  clear(teamId?: string): void {
    if (teamId === undefined) {
      this.entries.clear();
      return;
    }

    const prefix = `${teamId}|`;

    for (const key of [...this.entries.keys()]) {
      if (key.startsWith(prefix)) {
        this.entries.delete(key);
      }
    }
  }
}

function cacheKey(teamId: string, weekStart: IsoDate): string {
  return `${teamId}|${weekStart}`;
}
