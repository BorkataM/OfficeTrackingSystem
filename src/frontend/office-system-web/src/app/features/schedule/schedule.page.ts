import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AttendanceStatus, IsoDate, TeamSchedule } from '../../core/api/api.models';
import { errorCodeOf, errorMessageOf } from '../../core/api/problem-details';
import { ScheduleApi } from '../../core/api/schedule.api';
import { SessionStore } from '../../core/auth/session.store';
import {
  addDays,
  formatRange,
  isWeekend,
  isoWeekNumber,
  startOfWeek,
  todayIso,
} from '../../core/dates';
import { LatestRequest } from '../../core/latest-request';
import { TeamStore } from '../../core/teams/team.store';
import { ToastService } from '../../core/ui/toast.service';
import { STATUS_DESCRIPTORS, describeStatus } from '../../shared/attendance';
import { Avatar } from '../../shared/avatar/avatar';
import { Icon } from '../../shared/icon/icon';
import { AnchoredTo } from '../../shared/popover/anchored-to';
import { StatusChoice, StatusPicker } from '../../shared/status-picker/status-picker';
import { DayRosterPanel } from './day-roster-panel/day-roster-panel';
import { PlanChange, partitionChanges, withPlanChanges } from './plan-mutations';
import { ScheduleCache } from './schedule-cache';
import { ScheduleView, buildScheduleView } from './schedule.view-model';
import { AttendanceStatisticsPanel } from './statistics/attendance-statistics';

/** The day being edited, together with the cell the picker hangs off. */
interface PickerTarget {
  readonly date: IsoDate;
  readonly anchor: HTMLElement;
}

/**
 * Clicks on these keep an open overlay alive: the popover's own controls, the cell
 * that opens the next popover, and the quick-actions trigger. A full-screen catcher
 * would swallow that second click and force the user to click twice to move between
 * days.
 */
const OVERLAY_SAFE_SELECTOR = '.picker-layer, .slot-button, .quick';

@Component({
  selector: 'app-schedule-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Avatar, Icon, StatusPicker, DayRosterPanel, AnchoredTo, AttendanceStatisticsPanel],
  templateUrl: './schedule.page.html',
  styleUrl: './schedule.page.scss',
  host: {
    '(document:pointerdown)': 'onDocumentPointerDown($event)',
    '(document:keydown.escape)': 'closeOverlays()',
  },
})
export class SchedulePage {
  private readonly api = inject(ScheduleApi);
  private readonly session = inject(SessionStore);
  private readonly toasts = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly teams = inject(TeamStore);
  protected readonly statuses = STATUS_DESCRIPTORS;
  protected readonly describe = describeStatus;

  private readonly cache = inject(ScheduleCache);
  private readonly scheduleRequest = new LatestRequest();

  protected readonly weekStart = signal<IsoDate>(startOfWeek(todayIso()));
  protected readonly includeWeekends = signal(false);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly schedule = signal<TeamSchedule | null>(null);

  /** The cell whose picker is open, or null. */
  protected readonly editing = signal<PickerTarget | null>(null);
  protected readonly quickOpen = signal(false);
  protected readonly selectedDay = signal<IsoDate | null>(null);

  /** The server's idea of today, which the picker labels days against. */
  protected readonly today = computed(() => this.schedule()?.today ?? todayIso());

  /**
   * Bumped after every successful write. The statistics panel covers a wider range
   * than the visible week, so it cannot share the grid's optimistic patch — it
   * refetches in the background instead.
   */
  protected readonly dataVersion = signal(0);

  protected readonly weekEnd = computed(() => addDays(this.weekStart(), 6));
  protected readonly weekLabel = computed(() => formatRange(this.weekStart(), this.weekEnd()));
  protected readonly weekNumber = computed(() => isoWeekNumber(this.weekStart()));
  protected readonly isCurrentWeek = computed(() => this.weekStart() === startOfWeek(todayIso()));

  protected readonly view = computed<ScheduleView | null>(() => {
    const schedule = this.schedule();

    // Only render a schedule that covers the week the header is showing. A response
    // for a week the user has already navigated away from is treated as "not loaded
    // yet", so the header and the grid can never disagree about which week is up.
    if (!schedule || schedule.start !== this.weekStart()) {
      return null;
    }

    return buildScheduleView(schedule, this.session.user()?.id ?? null, this.includeWeekends());
  });

  protected readonly myRow = computed(() => this.view()?.rows.find((row) => row.isMe) ?? null);

  /** Column count drives the grid template; kept here so the template stays declarative. */
  protected readonly columnCount = computed(() => this.view()?.columns.length ?? 0);

  constructor() {
    // A team must be chosen before a schedule exists to show.
    effect(() => {
      if (this.teams.loaded() && !this.teams.selectedId()) {
        void this.router.navigate(['/teams']);
      }
    });

    effect(() => {
      // Deliberately does NOT wait for the team list. The remembered team id is
      // available from the first tick, so the schedule request goes out in parallel
      // with /api/teams instead of a round trip behind it. If the remembered team
      // turns out to be stale the request 403s, which reload() swallows — the team
      // list's own reconciliation redirects to /teams.
      const teamId = this.teams.selectedId();
      const from = this.weekStart();

      if (teamId) {
        void this.reload(teamId, from);
      }
    });

    void this.teams.load();
  }

  protected previousWeek(): void {
    this.closeOverlays();
    this.weekStart.update((start) => addDays(start, -7));
  }

  protected nextWeek(): void {
    this.closeOverlays();
    this.weekStart.update((start) => addDays(start, 7));
  }

  protected thisWeek(): void {
    this.closeOverlays();
    this.weekStart.set(startOfWeek(todayIso()));
  }

  protected toggleWeekends(): void {
    this.closeOverlays();
    this.includeWeekends.update((include) => !include);
  }

  protected openPicker(date: IsoDate, anchor: HTMLElement): void {
    this.quickOpen.set(false);
    this.editing.update((current) => (current?.date === date ? null : { date, anchor }));
  }

  protected closeOverlays(): void {
    this.editing.set(null);
    this.quickOpen.set(false);
  }

  protected onDocumentPointerDown(event: Event): void {
    const target = event.target as Element | null;

    if (target?.closest(OVERLAY_SAFE_SELECTOR)) {
      return;
    }

    this.closeOverlays();
  }

  protected planFor(date: IsoDate): { status: AttendanceStatus | null; note: string | null } {
    const cell = this.myRow()?.cells.find((candidate) => candidate.date === date);

    return { status: cell?.status ?? null, note: cell?.note ?? null };
  }

  protected async pick(date: IsoDate, choice: StatusChoice): Promise<void> {
    await this.applyChanges([{ date, status: choice.status, note: choice.note }]);
  }

  protected async clear(date: IsoDate): Promise<void> {
    await this.applyChanges([{ date, status: null, note: null }]);
  }

  /** "Mark every weekday" — the action that makes a whole week one click. */
  protected async fillWeekdays(status: AttendanceStatus): Promise<void> {
    this.quickOpen.set(false);

    await this.applyChanges(
      this.weekdayDates().map((date) => ({ date, status, note: null })),
      `Weekdays marked as ${describeStatus(status).label.toLowerCase()}.`,
    );
  }

  protected async clearWeek(): Promise<void> {
    this.quickOpen.set(false);

    await this.applyChanges(
      this.weekdayDates().map((date) => ({ date, status: null, note: null })),
      'Your week has been cleared.',
    );
  }

  /**
   * Repeats the previous week's plan onto this one. Unlike the other actions this
   * one cannot paint first: it has to read last week before it knows what to write.
   */
  protected async copyPreviousWeek(): Promise<void> {
    const teamId = this.teams.selectedId();

    if (!teamId) {
      return;
    }

    this.quickOpen.set(false);
    this.saving.set(true);

    try {
      const previousStart = addDays(this.weekStart(), -7);
      const previous = await firstValueFrom(
        this.api.getMyPlan(teamId, previousStart, addDays(previousStart, 6)),
      );

      if (previous.length === 0) {
        this.toasts.info('There was nothing planned last week to copy.');
        return;
      }

      await this.applyChanges(
        previous.map((plan) => ({
          date: addDays(plan.date, 7),
          status: plan.status,
          note: plan.note,
        })),
        'Last week copied over.',
      );
    } catch (error) {
      this.toasts.error(errorMessageOf(error));
    } finally {
      this.saving.set(false);
    }
  }

  protected selectDay(date: IsoDate): void {
    this.selectedDay.update((current) => (current === date ? null : date));
  }

  private weekdayDates(): IsoDate[] {
    return Array.from({ length: 7 }, (_, offset) => addDays(this.weekStart(), offset)).filter(
      (date) => !isWeekend(date),
    );
  }

  /**
   * Repaints the grid immediately, then sends the change. The request confirms the
   * edit rather than granting it, so the cell never waits on the network. If the
   * server refuses, its view of the week is reloaded and the change disappears.
   */
  private async applyChanges(changes: readonly PlanChange[], successMessage?: string): Promise<void> {
    const teamId = this.teams.selectedId();
    const userId = this.session.user()?.id;
    const loaded = this.schedule();

    if (!teamId || !userId || changes.length === 0) {
      return;
    }

    this.editing.set(null);

    if (loaded) {
      this.schedule.set(withPlanChanges(loaded, userId, changes));
    }

    // Keep every cached week in step, so navigating away and back does not show
    // the pre-edit copy.
    this.cache.patch(teamId, userId, changes);

    const { toSet, toClear } = partitionChanges(changes);

    try {
      await Promise.all([
        toSet.length > 0 ? firstValueFrom(this.api.setMyPlan(teamId, toSet)) : Promise.resolve(),
        toClear.length > 0 ? firstValueFrom(this.api.clearMyPlan(teamId, toClear)) : Promise.resolve(),
      ]);

      this.dataVersion.update((version) => version + 1);

      if (successMessage) {
        this.toasts.success(successMessage);
      }
    } catch (error) {
      this.toasts.error(errorMessageOf(error));

      // The server rejected it, so drop what we guessed and take its word.
      this.cache.clear(teamId);
      await this.reload(teamId, this.weekStart());
      this.dataVersion.update((version) => version + 1);
    }
  }

  /**
   * Stale-while-revalidate: a cached week paints immediately and the request becomes
   * a background refresh, so navigation costs nothing. Only an unseen week shows the
   * loading state.
   */
  private async reload(teamId: string, weekStart: IsoDate): Promise<void> {
    const token = this.scheduleRequest.begin();
    const cached = this.cache.get(teamId, weekStart);

    this.error.set(null);

    if (cached) {
      this.schedule.set(cached);
    } else {
      this.loading.set(true);
    }

    try {
      const schedule = await firstValueFrom(
        this.api.getSchedule(teamId, weekStart, addDays(weekStart, 6)),
      );

      this.cache.set(teamId, schedule);

      if (this.scheduleRequest.isCurrent(token)) {
        this.schedule.set(schedule);

        // Keep the detail panel on a day that is still on screen.
        const day = this.selectedDay();

        if (day && !schedule.days.includes(day)) {
          this.selectedDay.set(null);
        }
      }
    } catch (error) {
      if (!this.scheduleRequest.isCurrent(token)) {
        return;
      }

      // A remembered team we are no longer in is resolved by the redirect to
      // /teams, so it must not raise an error card on the way there.
      const code = errorCodeOf(error);

      if (code === 'team.notMember' || code === 'team.notFound') {
        this.schedule.set(null);
      } else if (!cached) {
        this.schedule.set(null);
        this.error.set(errorMessageOf(error));
      }
      // With a cached week already on screen, a failed refresh stays silent.
    } finally {
      // A superseded request must not clear the spinner the newest one is still using.
      if (this.scheduleRequest.isCurrent(token)) {
        this.loading.set(false);
      }
    }

    this.prefetchNeighbours(teamId, weekStart);
  }

  /**
   * Warms the weeks either side. They are ~5&nbsp;kB each, so paying for them up
   * front makes the arrows instant even the first time.
   */
  private prefetchNeighbours(teamId: string, weekStart: IsoDate): void {
    for (const offset of [7, -7]) {
      const start = addDays(weekStart, offset);

      if (this.cache.has(teamId, start)) {
        continue;
      }

      firstValueFrom(this.api.getSchedule(teamId, start, addDays(start, 6)))
        .then((schedule) => this.cache.set(teamId, schedule))
        // A prefetch that fails is not the user's problem; the real request will report it.
        .catch(() => undefined);
    }
  }
}
