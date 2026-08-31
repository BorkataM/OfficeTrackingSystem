import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { IsoDate, TeamSchedule } from '../../../core/api/api.models';
import { errorMessageOf } from '../../../core/api/problem-details';
import { ScheduleApi } from '../../../core/api/schedule.api';
import { formatDayMonth, formatWeekdayDayMonth } from '../../../core/dates';
import { LatestRequest } from '../../../core/latest-request';
import { Avatar } from '../../../shared/avatar/avatar';
import { Column, ColumnChart } from '../../../shared/charts/column-chart';
import { DonutChart, DonutSlice } from '../../../shared/charts/donut-chart';
import { TrendChart, TrendPoint } from '../../../shared/charts/trend-chart';
import { Icon } from '../../../shared/icon/icon';
import {
  AttendanceStatistics,
  PERIOD_OPTIONS,
  StatisticsPeriod,
  periodRange,
  summarise,
} from './attendance-statistics.model';

/** Legend rows carry the value, so no figure here depends on hovering. */
interface LegendRow extends DonutSlice {
  readonly percentage: number;
}

@Component({
  selector: 'app-attendance-statistics',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Avatar, Icon, DonutChart, ColumnChart, TrendChart],
  templateUrl: './attendance-statistics.html',
  styleUrl: './attendance-statistics.scss',
})
export class AttendanceStatisticsPanel {
  private readonly api = inject(ScheduleApi);
  private readonly request = new LatestRequest();

  readonly teamId = input.required<string>();
  /** Bumped by the page after a write, so the figures follow an edit. */
  readonly reloadToken = input<number>(0);

  protected readonly periods = PERIOD_OPTIONS;
  protected readonly period = signal<StatisticsPeriod>('last30');
  protected readonly loading = signal(false);
  protected readonly refreshing = signal(false);
  protected readonly error = signal<string | null>(null);

  private readonly schedule = signal<TeamSchedule | null>(null);

  protected readonly stats = computed<AttendanceStatistics | null>(() => {
    const schedule = this.schedule();

    return schedule ? summarise(schedule) : null;
  });

  protected readonly rangeLabel = computed(() => {
    const stats = this.stats();

    return stats ? `${formatDayMonth(stats.from)} – ${formatDayMonth(stats.to)}` : '';
  });

  protected readonly legend = computed<readonly LegendRow[]>(() => {
    const stats = this.stats();

    if (!stats) {
      return [];
    }

    const rows: Omit<LegendRow, 'percentage'>[] = [
      { key: 'Office', label: 'In office', value: stats.totals.office, color: 'var(--chart-office)' },
      { key: 'Remote', label: 'Remote', value: stats.totals.remote, color: 'var(--chart-remote)' },
      { key: 'Travelling', label: 'Travelling', value: stats.totals.travelling, color: 'var(--chart-travelling)' },
      { key: 'Away', label: 'Away', value: stats.totals.away, color: 'var(--chart-away)' },
    ];

    const total = Math.max(1, stats.planned);

    return rows.map((row) => ({ ...row, percentage: Math.round((row.value / total) * 100) }));
  });

  protected readonly weekdayColumns = computed<readonly Column[]>(() =>
    (this.stats()?.weekdays ?? []).map((weekday) => ({
      label: weekday.label,
      value: weekday.averageInOffice,
      display: weekday.averageInOffice.toFixed(1),
    })),
  );

  protected readonly trendPoints = computed<readonly TrendPoint[]>(() =>
    (this.stats()?.daily ?? []).map((point) => ({
      key: point.date,
      label: formatDayMonth(point.date),
      value: point.inOffice,
    })),
  );

  /** Longest bar in the leaderboard, used to scale the rest. */
  protected readonly leaderMax = computed(() =>
    Math.max(1, ...(this.stats()?.members ?? []).map((member) => member.office)),
  );

  constructor() {
    effect(() => {
      const teamId = this.teamId();
      const period = this.period();

      // Read so an edit on the page refreshes the figures.
      this.reloadToken();

      // The work runs untracked. load() both reads and writes `schedule`, so
      // leaving it tracked makes every response retrigger this effect — an endless
      // refetch loop.
      untracked(() => void this.load(teamId, period));
    });
  }

  protected selectPeriod(period: StatisticsPeriod): void {
    this.period.set(period);
  }

  protected barWidth(officeDays: number): number {
    return (officeDays / this.leaderMax()) * 100;
  }

  protected percent(fraction: number): string {
    return `${Math.round(fraction * 100)}%`;
  }

  protected dayLabel(date: IsoDate): string {
    return formatWeekdayDayMonth(date);
  }

  private async load(teamId: string, period: StatisticsPeriod): Promise<void> {
    const token = this.request.begin();
    const { from, to } = periodRange(period);

    // Hold the previous render while refetching instead of flashing a skeleton.
    if (this.schedule() === null) {
      this.loading.set(true);
    } else {
      this.refreshing.set(true);
    }

    this.error.set(null);

    try {
      const schedule = await firstValueFrom(this.api.getSchedule(teamId, from, to));

      if (this.request.isCurrent(token)) {
        this.schedule.set(schedule);
      }
    } catch (error) {
      if (this.request.isCurrent(token)) {
        this.schedule.set(null);
        this.error.set(errorMessageOf(error));
      }
    } finally {
      if (this.request.isCurrent(token)) {
        this.loading.set(false);
        this.refreshing.set(false);
      }
    }
  }
}
