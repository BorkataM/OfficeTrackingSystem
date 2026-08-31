import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AttendanceStatus, DayPlan, IsoDate } from '../../core/api/api.models';
import { errorMessageOf } from '../../core/api/problem-details';
import { ScheduleApi } from '../../core/api/schedule.api';
import {
  addDays,
  addMonths,
  endOfMonth,
  formatDayOfMonth,
  formatMonthYear,
  formatWeekdayShort,
  isWeekend,
  isoRange,
  startOfMonth,
  startOfWeek,
  todayIso,
} from '../../core/dates';
import { LatestRequest } from '../../core/latest-request';
import { TeamStore } from '../../core/teams/team.store';
import { ToastService } from '../../core/ui/toast.service';
import { STATUS_DESCRIPTORS, describeStatus } from '../../shared/attendance';
import { Icon } from '../../shared/icon/icon';
import { AnchoredTo } from '../../shared/popover/anchored-to';
import { StatusChoice, StatusPicker } from '../../shared/status-picker/status-picker';
import { PlanChange, partitionChanges, withDayPlanChanges } from './plan-mutations';

interface MonthCell {
  readonly date: IsoDate;
  readonly dayOfMonth: string;
  readonly inMonth: boolean;
  readonly isToday: boolean;
  readonly isWeekend: boolean;
  readonly status: AttendanceStatus | null;
  readonly note: string | null;
}

interface StatusTally {
  readonly status: AttendanceStatus;
  readonly label: string;
  readonly cssClass: string;
  readonly count: number;
}

/** The day being edited, together with the cell the picker hangs off. */
interface PickerTarget {
  readonly date: IsoDate;
  readonly anchor: HTMLElement;
}

/** See SchedulePage: clicking straight from one day to the next must not need two clicks. */
const OVERLAY_SAFE_SELECTOR = '.picker-layer, .day-button';

@Component({
  selector: 'app-my-month-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon, StatusPicker, AnchoredTo],
  templateUrl: './my-month.page.html',
  styleUrl: './my-month.page.scss',
  host: {
    '(document:pointerdown)': 'onDocumentPointerDown($event)',
    '(document:keydown.escape)': 'editing.set(null)',
  },
})
export class MyMonthPage {
  private readonly api = inject(ScheduleApi);
  private readonly toasts = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly teams = inject(TeamStore);
  protected readonly describe = describeStatus;

  private readonly planRequest = new LatestRequest();

  protected readonly anchor = signal<IsoDate>(startOfMonth(todayIso()));
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly plans = signal<readonly DayPlan[]>([]);
  protected readonly editing = signal<PickerTarget | null>(null);

  protected readonly today = todayIso();
  protected readonly monthLabel = computed(() => formatMonthYear(this.anchor()));
  protected readonly isCurrentMonth = computed(() => this.anchor() === startOfMonth(this.today));

  /** Weekday headings in the user's locale, starting on Monday. */
  protected readonly weekdayNames = computed(() => {
    const monday = startOfWeek(this.today);

    return Array.from({ length: 7 }, (_, offset) => formatWeekdayShort(addDays(monday, offset)));
  });

  /**
   * Six weeks starting on the Monday on or before the 1st, so the grid never
   * changes height between months.
   */
  protected readonly cells = computed<readonly MonthCell[]>(() => {
    const monthStart = startOfMonth(this.anchor());
    const monthEnd = endOfMonth(this.anchor());
    const gridStart = startOfWeek(monthStart);
    const planByDate = new Map(this.plans().map((plan) => [plan.date, plan]));

    return isoRange(gridStart, addDays(gridStart, 41)).map((date) => {
      const plan = planByDate.get(date);

      return {
        date,
        dayOfMonth: formatDayOfMonth(date),
        inMonth: date >= monthStart && date <= monthEnd,
        isToday: date === this.today,
        isWeekend: isWeekend(date),
        status: plan?.status ?? null,
        note: plan?.note ?? null,
      };
    });
  });

  protected readonly tallies = computed<readonly StatusTally[]>(() => {
    const monthStart = startOfMonth(this.anchor());
    const monthEnd = endOfMonth(this.anchor());
    const inMonth = this.plans().filter((plan) => plan.date >= monthStart && plan.date <= monthEnd);

    return STATUS_DESCRIPTORS.map((descriptor) => ({
      status: descriptor.status,
      label: descriptor.label,
      cssClass: descriptor.cssClass,
      count: inMonth.filter((plan) => plan.status === descriptor.status).length,
    }));
  });

  protected readonly plannedCount = computed(() => this.tallies().reduce((sum, tally) => sum + tally.count, 0));

  constructor() {
    effect(() => {
      if (this.teams.loaded() && !this.teams.selectedId()) {
        void this.router.navigate(['/teams']);
      }
    });

    effect(() => {
      // See SchedulePage: only load once the remembered selection is reconciled.
      if (!this.teams.loaded()) {
        return;
      }

      const teamId = this.teams.selectedId();
      const anchor = this.anchor();

      if (teamId) {
        void this.reload(teamId, anchor);
      }
    });

    void this.teams.load();
  }

  protected previousMonth(): void {
    this.editing.set(null);
    this.anchor.update((current) => startOfMonth(addMonths(current, -1)));
  }

  protected nextMonth(): void {
    this.editing.set(null);
    this.anchor.update((current) => startOfMonth(addMonths(current, 1)));
  }

  protected thisMonth(): void {
    this.editing.set(null);
    this.anchor.set(startOfMonth(this.today));
  }

  protected openPicker(cell: MonthCell, anchor: HTMLElement): void {
    if (!cell.inMonth) {
      return;
    }

    this.editing.update((current) => (current?.date === cell.date ? null : { date: cell.date, anchor }));
  }

  protected onDocumentPointerDown(event: Event): void {
    const target = event.target as Element | null;

    if (!target?.closest(OVERLAY_SAFE_SELECTOR)) {
      this.editing.set(null);
    }
  }

  protected planFor(date: IsoDate): { status: AttendanceStatus | null; note: string | null } {
    const plan = this.plans().find((candidate) => candidate.date === date);

    return { status: plan?.status ?? null, note: plan?.note ?? null };
  }

  protected async pick(date: IsoDate, choice: StatusChoice): Promise<void> {
    await this.applyChanges([{ date, status: choice.status, note: choice.note }]);
  }

  protected async clear(date: IsoDate): Promise<void> {
    await this.applyChanges([{ date, status: null, note: null }]);
  }

  /** See SchedulePage: the day repaints at once and the request only confirms it. */
  private async applyChanges(changes: readonly PlanChange[]): Promise<void> {
    const teamId = this.teams.selectedId();

    if (!teamId || changes.length === 0) {
      return;
    }

    this.editing.set(null);
    this.plans.update((plans) => withDayPlanChanges(plans, changes));

    const { toSet, toClear } = partitionChanges(changes);

    try {
      await Promise.all([
        toSet.length > 0 ? firstValueFrom(this.api.setMyPlan(teamId, toSet)) : Promise.resolve(),
        toClear.length > 0 ? firstValueFrom(this.api.clearMyPlan(teamId, toClear)) : Promise.resolve(),
      ]);
    } catch (error) {
      this.toasts.error(errorMessageOf(error));
      await this.reload(teamId, this.anchor());
    }
  }

  private async reload(teamId: string, anchor: IsoDate): Promise<void> {
    const token = this.planRequest.begin();

    this.loading.set(true);
    this.error.set(null);

    // The grid shows leading and trailing days, so the query covers them too.
    const gridStart = startOfWeek(startOfMonth(anchor));

    try {
      const plans = await firstValueFrom(this.api.getMyPlan(teamId, gridStart, addDays(gridStart, 41)));

      // A response for a month the user has already paged past must not be applied.
      if (this.planRequest.isCurrent(token)) {
        this.plans.set(plans);
      }
    } catch (error) {
      if (this.planRequest.isCurrent(token)) {
        this.plans.set([]);
        this.error.set(errorMessageOf(error));
      }
    } finally {
      if (this.planRequest.isCurrent(token)) {
        this.loading.set(false);
      }
    }
  }
}
