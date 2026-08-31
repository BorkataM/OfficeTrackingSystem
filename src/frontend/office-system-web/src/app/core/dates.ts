import { IsoDate } from './api/api.models';

/**
 * All date arithmetic goes through `yyyy-MM-dd` strings and UTC-noon Date objects.
 * Using noon avoids the classic off-by-one where a midnight timestamp in a negative
 * UTC offset lands on the previous day.
 */

export function toIsoDate(date: Date): IsoDate {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');

  return `${year}-${month}-${day}`;
}

export function parseIsoDate(iso: IsoDate): Date {
  const [year, month, day] = iso.split('-').map(Number);

  return new Date(year, (month ?? 1) - 1, day ?? 1, 12);
}

export function todayIso(): IsoDate {
  return toIsoDate(new Date());
}

export function addDays(iso: IsoDate, days: number): IsoDate {
  const date = parseIsoDate(iso);
  date.setDate(date.getDate() + days);

  return toIsoDate(date);
}

export function addMonths(iso: IsoDate, months: number): IsoDate {
  const date = parseIsoDate(iso);
  const targetMonth = date.getMonth() + months;
  const anchor = new Date(date.getFullYear(), targetMonth, 1, 12);

  // Clamp so 31 January + 1 month lands on 28/29 February, not on 3 March.
  const lastDayOfTarget = new Date(anchor.getFullYear(), anchor.getMonth() + 1, 0, 12).getDate();
  anchor.setDate(Math.min(date.getDate(), lastDayOfTarget));

  return toIsoDate(anchor);
}

/** Monday of the ISO week containing `iso`. */
export function startOfWeek(iso: IsoDate): IsoDate {
  const date = parseIsoDate(iso);
  const offsetToMonday = (date.getDay() + 6) % 7;

  return addDays(iso, -offsetToMonday);
}

export function startOfMonth(iso: IsoDate): IsoDate {
  const date = parseIsoDate(iso);

  return toIsoDate(new Date(date.getFullYear(), date.getMonth(), 1, 12));
}

export function endOfMonth(iso: IsoDate): IsoDate {
  const date = parseIsoDate(iso);

  return toIsoDate(new Date(date.getFullYear(), date.getMonth() + 1, 0, 12));
}

export function isWeekend(iso: IsoDate): boolean {
  const day = parseIsoDate(iso).getDay();

  return day === 0 || day === 6;
}

export function isoRange(from: IsoDate, to: IsoDate): IsoDate[] {
  const days: IsoDate[] = [];

  for (let cursor = from; cursor <= to; cursor = addDays(cursor, 1)) {
    days.push(cursor);
  }

  return days;
}

/** ISO week number, used as a compact label above the grid. */
export function isoWeekNumber(iso: IsoDate): number {
  const date = parseIsoDate(iso);
  const thursday = new Date(date.getFullYear(), date.getMonth(), date.getDate(), 12);
  thursday.setDate(thursday.getDate() + 3 - ((thursday.getDay() + 6) % 7));

  const firstThursday = new Date(thursday.getFullYear(), 0, 4, 12);
  firstThursday.setDate(firstThursday.getDate() + 3 - ((firstThursday.getDay() + 6) % 7));

  return 1 + Math.round((thursday.getTime() - firstThursday.getTime()) / (7 * 24 * 60 * 60 * 1000));
}

const WEEKDAY_SHORT = new Intl.DateTimeFormat(undefined, { weekday: 'short' });
const WEEKDAY_LONG = new Intl.DateTimeFormat(undefined, { weekday: 'long' });
const DAY_MONTH = new Intl.DateTimeFormat(undefined, { day: 'numeric', month: 'short' });
const DAY_MONTH_LONG = new Intl.DateTimeFormat(undefined, { day: 'numeric', month: 'long' });
const MONTH_YEAR = new Intl.DateTimeFormat(undefined, { month: 'long', year: 'numeric' });
const WEEKDAY_DAY_MONTH = new Intl.DateTimeFormat(undefined, {
  weekday: 'short',
  day: 'numeric',
  month: 'short',
});

export function formatWeekdayShort(iso: IsoDate): string {
  return WEEKDAY_SHORT.format(parseIsoDate(iso));
}

export function formatWeekdayLong(iso: IsoDate): string {
  return WEEKDAY_LONG.format(parseIsoDate(iso));
}

export function formatDayOfMonth(iso: IsoDate): string {
  return `${parseIsoDate(iso).getDate()}`;
}

export function formatDayMonth(iso: IsoDate): string {
  return DAY_MONTH.format(parseIsoDate(iso));
}

export function formatDayMonthLong(iso: IsoDate): string {
  return DAY_MONTH_LONG.format(parseIsoDate(iso));
}

export function formatMonthYear(iso: IsoDate): string {
  return MONTH_YEAR.format(parseIsoDate(iso));
}

/** "Mon 31 Aug" — used where a date needs naming outside a calendar grid. */
export function formatWeekdayDayMonth(iso: IsoDate): string {
  return WEEKDAY_DAY_MONTH.format(parseIsoDate(iso));
}

/**
 * "24 – 30 Aug", or "28 Aug – 3 Sep" across a month boundary. Delegated to Intl so
 * the shared month is collapsed on the side the locale expects.
 */
export function formatRange(from: IsoDate, to: IsoDate): string {
  return DAY_MONTH.formatRange(parseIsoDate(from), parseIsoDate(to));
}

/** "today", "tomorrow", "yesterday", or a plain date. */
export function describeRelativeDay(iso: IsoDate, today: IsoDate): string | null {
  const difference = parseIsoDate(iso).getTime() - parseIsoDate(today).getTime();
  const days = Math.round(difference / (24 * 60 * 60 * 1000));

  switch (days) {
    case 0:
      return 'Today';
    case 1:
      return 'Tomorrow';
    case -1:
      return 'Yesterday';
    default:
      return null;
  }
}
