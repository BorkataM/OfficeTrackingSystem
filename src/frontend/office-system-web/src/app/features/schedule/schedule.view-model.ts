import {
  AttendanceStatus,
  DayTotals,
  IsoDate,
  TeamMember,
  TeamSchedule,
} from '../../core/api/api.models';
import { formatDayOfMonth, formatWeekdayShort } from '../../core/dates';

export interface DayColumn {
  readonly date: IsoDate;
  readonly weekday: string;
  readonly dayOfMonth: string;
  readonly isToday: boolean;
  readonly isWeekend: boolean;
  readonly isPast: boolean;
  readonly totals: DayTotals;
  /** Share of the team in the office, 0–1, for the header's occupancy bar. */
  readonly officeShare: number;
}

export interface ScheduleCell {
  readonly date: IsoDate;
  readonly status: AttendanceStatus | null;
  readonly note: string | null;
  readonly isWeekend: boolean;
  readonly isToday: boolean;
}

export interface ScheduleRowView {
  readonly member: TeamMember;
  readonly isMe: boolean;
  readonly cells: readonly ScheduleCell[];
  readonly officeDays: number;
  readonly plannedDays: number;
}

export interface ScheduleView {
  readonly today: IsoDate;
  readonly columns: readonly DayColumn[];
  readonly rows: readonly ScheduleRowView[];
  readonly memberCount: number;
}

/**
 * Turns the API's sparse response into the dense grid the template renders, so the
 * template itself contains no lookups or arithmetic.
 */
export function buildScheduleView(
  schedule: TeamSchedule,
  currentUserId: string | null,
  includeWeekends: boolean,
): ScheduleView {
  const totalsByDate = new Map(schedule.totals.map((total) => [total.date, total]));

  const dates = schedule.days.filter(
    (date) => includeWeekends || totalsByDate.get(date)?.isWeekend !== true,
  );

  const memberCount = schedule.rows.length;

  const columns: DayColumn[] = dates.map((date) => {
    const totals = totalsByDate.get(date) ?? emptyTotals(date, memberCount);

    return {
      date,
      weekday: formatWeekdayShort(date),
      dayOfMonth: formatDayOfMonth(date),
      isToday: date === schedule.today,
      isWeekend: totals.isWeekend,
      isPast: date < schedule.today,
      totals,
      officeShare: memberCount === 0 ? 0 : totals.inOffice / memberCount,
    };
  });

  const rows: ScheduleRowView[] = schedule.rows.map((row) => {
    const planByDate = new Map(row.days.map((day) => [day.date, day]));

    const cells: ScheduleCell[] = dates.map((date) => {
      const plan = planByDate.get(date);

      return {
        date,
        status: plan?.status ?? null,
        note: plan?.note ?? null,
        isWeekend: totalsByDate.get(date)?.isWeekend ?? false,
        isToday: date === schedule.today,
      };
    });

    return {
      member: row.member,
      isMe: row.member.userId === currentUserId,
      cells,
      officeDays: cells.filter((cell) => cell.status === 'Office').length,
      plannedDays: cells.filter((cell) => cell.status !== null).length,
    };
  });

  // The signed-in user reads their own row first.
  rows.sort((left, right) => Number(right.isMe) - Number(left.isMe));

  return { today: schedule.today, columns, rows, memberCount };
}

function emptyTotals(date: IsoDate, memberCount: number): DayTotals {
  return {
    date,
    isWeekend: false,
    inOffice: 0,
    remote: 0,
    travelling: 0,
    away: 0,
    notPlanned: memberCount,
  };
}
