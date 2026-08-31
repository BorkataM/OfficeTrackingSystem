import {
  AttendanceStatus,
  DayPlan,
  DayPlanInput,
  DayTotals,
  IsoDate,
  ScheduleRow,
  TeamSchedule,
} from '../../core/api/api.models';

/** One pending edit. A null status clears the day. */
export interface PlanChange {
  readonly date: IsoDate;
  readonly status: AttendanceStatus | null;
  readonly note: string | null;
}

/**
 * Applies edits to a loaded schedule so the grid can repaint before the request
 * finishes. Totals are recomputed, so occupancy counts and the header bars stay
 * consistent with the cells.
 */
export function withPlanChanges(
  schedule: TeamSchedule,
  userId: string,
  changes: readonly PlanChange[],
): TeamSchedule {
  const inRange = changes.filter((change) => schedule.days.includes(change.date));

  if (inRange.length === 0) {
    return schedule;
  }

  const rows = schedule.rows.map((row) =>
    row.member.userId === userId ? { ...row, days: withDayPlanChanges(row.days, inRange) } : row,
  );

  const touched = new Set(inRange.map((change) => change.date));

  const totals = schedule.totals.map((total) => (touched.has(total.date) ? recount(total, rows) : total));

  return { ...schedule, rows, totals };
}

/** The same edits against a flat list of the caller's own days, for the month view. */
export function withDayPlanChanges(
  plans: readonly DayPlan[],
  changes: readonly PlanChange[],
): DayPlan[] {
  const byDate = new Map(plans.map((plan) => [plan.date, plan]));

  for (const change of changes) {
    if (change.status === null) {
      byDate.delete(change.date);
      continue;
    }

    byDate.set(change.date, {
      date: change.date,
      status: change.status,
      note: change.note,
      // Never read by the UI; the server's value arrives with the next load.
      updatedAtUtc: new Date().toISOString(),
    });
  }

  return [...byDate.values()].sort((left, right) => left.date.localeCompare(right.date));
}

/** Splits edits into the two calls the API offers. */
export function partitionChanges(changes: readonly PlanChange[]): {
  readonly toSet: readonly DayPlanInput[];
  readonly toClear: readonly IsoDate[];
} {
  const toSet: DayPlanInput[] = [];
  const toClear: IsoDate[] = [];

  for (const change of changes) {
    if (change.status === null) {
      toClear.push(change.date);
    } else {
      toSet.push({ date: change.date, status: change.status, note: change.note });
    }
  }

  return { toSet, toClear };
}

/**
 * Counts a day from the already-patched rows rather than nudging the previous
 * counters: one code path, and it cannot drift out of step with the cells.
 */
function recount(previous: DayTotals, rows: readonly ScheduleRow[]): DayTotals {
  let inOffice = 0;
  let remote = 0;
  let travelling = 0;
  let away = 0;

  for (const row of rows) {
    switch (row.days.find((day) => day.date === previous.date)?.status) {
      case 'Office':
        inOffice++;
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

  return {
    ...previous,
    inOffice,
    remote,
    travelling,
    away,
    notPlanned: Math.max(0, rows.length - (inOffice + remote + travelling + away)),
  };
}
