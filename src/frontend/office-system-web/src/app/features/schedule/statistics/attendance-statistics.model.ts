import { AttendanceStatus, IsoDate, TeamSchedule } from '../../../core/api/api.models';
import { addDays, endOfMonth, formatWeekdayShort, isWeekend, startOfMonth, todayIso } from '../../../core/dates';

export type StatisticsPeriod = 'month' | 'last30' | 'last90';

export interface PeriodOption {
  readonly value: StatisticsPeriod;
  readonly label: string;
}

export const PERIOD_OPTIONS: readonly PeriodOption[] = [
  { value: 'month', label: 'This month' },
  { value: 'last30', label: 'Last 30 days' },
  { value: 'last90', label: 'Last 90 days' },
];

export interface StatusTotals {
  readonly office: number;
  readonly remote: number;
  readonly travelling: number;
  readonly away: number;
}

export interface MemberAttendance extends StatusTotals {
  readonly userId: string;
  readonly displayName: string;
  readonly accentColor: string;
  /** Days with any status set. */
  readonly planned: number;
  /** Office days as a share of the period's working days. */
  readonly officeRate: number;
  /** Office days as a share of the days this person actually planned. */
  readonly officeShareOfPlanned: number;
}

export interface WeekdayAttendance {
  readonly label: string;
  readonly occurrences: number;
  readonly averageInOffice: number;
}

export interface DayPoint {
  readonly date: IsoDate;
  readonly inOffice: number;
}

export interface AttendanceStatistics {
  readonly from: IsoDate;
  readonly to: IsoDate;
  /** Weekdays in the period. Weekends are excluded from every figure here. */
  readonly workingDays: number;
  readonly memberCount: number;
  readonly totals: StatusTotals;
  readonly planned: number;
  readonly notPlanned: number;
  /** memberCount × workingDays: every person-day the period could hold. */
  readonly capacity: number;
  readonly averageInOffice: number;
  readonly busiest: DayPoint | null;
  readonly quietest: DayPoint | null;
  readonly officeShareOfPlanned: number;
  readonly planningRate: number;
  /** Ranked by office days, descending — the "most in the office" order. */
  readonly members: readonly MemberAttendance[];
  readonly weekdays: readonly WeekdayAttendance[];
  readonly daily: readonly DayPoint[];
}

export function periodRange(period: StatisticsPeriod, today: IsoDate = todayIso()): { from: IsoDate; to: IsoDate } {
  switch (period) {
    case 'month':
      return { from: startOfMonth(today), to: endOfMonth(today) };
    case 'last30':
      return { from: addDays(today, -29), to: today };
    case 'last90':
      return { from: addDays(today, -89), to: today };
  }
}

/**
 * Rolls a loaded schedule into the figures the dashboard shows.
 *
 * Weekends are dropped first: including them would bury every average under days
 * nobody was ever going to plan, and make "not planned" mostly mean "Saturday".
 */
export function summarise(schedule: TeamSchedule): AttendanceStatistics {
  const workdays = schedule.days.filter((date) => !isWeekend(date));
  const workdaySet = new Set(workdays);
  const memberCount = schedule.rows.length;

  const members: MemberAttendance[] = schedule.rows
    .map((row) => {
      const counted = row.days.filter((day) => workdaySet.has(day.date));
      const totals = tally(counted.map((day) => day.status));
      const planned = totals.office + totals.remote + totals.travelling + totals.away;

      return {
        userId: row.member.userId,
        displayName: row.member.displayName,
        accentColor: row.member.accentColor,
        ...totals,
        planned,
        officeRate: workdays.length === 0 ? 0 : totals.office / workdays.length,
        officeShareOfPlanned: planned === 0 ? 0 : totals.office / planned,
      };
    })
    // Most office days first; ties broken by name so the order is stable.
    .sort((left, right) => right.office - left.office || left.displayName.localeCompare(right.displayName));

  const totals = members.reduce<StatusTotals>(
    (sum, member) => ({
      office: sum.office + member.office,
      remote: sum.remote + member.remote,
      travelling: sum.travelling + member.travelling,
      away: sum.away + member.away,
    }),
    { office: 0, remote: 0, travelling: 0, away: 0 },
  );

  const planned = totals.office + totals.remote + totals.travelling + totals.away;
  const capacity = memberCount * workdays.length;

  const daily: DayPoint[] = workdays.map((date) => ({
    date,
    inOffice: schedule.totals.find((total) => total.date === date)?.inOffice ?? 0,
  }));

  return {
    from: schedule.start,
    to: schedule.end,
    workingDays: workdays.length,
    memberCount,
    totals,
    planned,
    notPlanned: Math.max(0, capacity - planned),
    capacity,
    averageInOffice: daily.length === 0 ? 0 : totals.office / daily.length,
    busiest: extreme(daily, (candidate, best) => candidate.inOffice > best.inOffice),
    quietest: extreme(daily, (candidate, best) => candidate.inOffice < best.inOffice),
    officeShareOfPlanned: planned === 0 ? 0 : totals.office / planned,
    planningRate: capacity === 0 ? 0 : planned / capacity,
    members,
    weekdays: byWeekday(daily),
    daily,
  };
}

function tally(statuses: readonly AttendanceStatus[]): StatusTotals {
  let office = 0;
  let remote = 0;
  let travelling = 0;
  let away = 0;

  for (const status of statuses) {
    switch (status) {
      case 'Office':
        office++;
        break;
      case 'Remote':
        remote++;
        break;
      case 'Travelling':
        travelling++;
        break;
      case 'Away':
        away++;
        break;
      default:
        break;
    }
  }

  return { office, remote, travelling, away };
}

/** Average office headcount per weekday name, Monday first. */
function byWeekday(daily: readonly DayPoint[]): WeekdayAttendance[] {
  const buckets = new Map<string, { total: number; occurrences: number; order: number }>();

  for (const point of daily) {
    const date = new Date(`${point.date}T12:00:00`);
    // Monday first, matching the grid.
    const order = (date.getDay() + 6) % 7;
    const label = formatWeekdayShort(point.date);
    const bucket = buckets.get(label) ?? { total: 0, occurrences: 0, order };

    bucket.total += point.inOffice;
    bucket.occurrences += 1;
    buckets.set(label, bucket);
  }

  return [...buckets.entries()]
    .sort(([, left], [, right]) => left.order - right.order)
    .map(([label, bucket]) => ({
      label,
      occurrences: bucket.occurrences,
      averageInOffice: bucket.occurrences === 0 ? 0 : bucket.total / bucket.occurrences,
    }));
}

function extreme(points: readonly DayPoint[], better: (candidate: DayPoint, best: DayPoint) => boolean): DayPoint | null {
  let best: DayPoint | null = null;

  for (const point of points) {
    if (best === null || better(point, best)) {
      best = point;
    }
  }

  return best;
}
