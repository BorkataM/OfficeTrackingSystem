/** Mirrors the contracts in OfficeSystem.Application.Features.*.Contracts. */

export type AttendanceStatus = 'Office' | 'Remote' | 'Travelling' | 'Away';

export const ATTENDANCE_STATUSES: readonly AttendanceStatus[] = [
  'Office',
  'Remote',
  'Travelling',
  'Away',
] as const;

export type TeamRole = 'Member' | 'Admin' | 'Owner';

/** An ISO calendar date, `yyyy-MM-dd`. Kept as a string so it never drifts through a timezone. */
export type IsoDate = string;

export interface AuthenticatedUser {
  readonly id: string;
  readonly email: string;
  readonly displayName: string;
  readonly accentColor: string;
}

export interface AuthenticationResult {
  readonly accessToken: string;
  readonly accessTokenExpiresAtUtc: string;
  readonly refreshToken: string;
  readonly user: AuthenticatedUser;
}

export interface TeamListItem {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  readonly myRole: TeamRole;
  readonly memberCount: number;
  readonly createdAtUtc: string;
}

export interface TeamMember {
  readonly userId: string;
  readonly displayName: string;
  readonly email: string;
  readonly accentColor: string;
  readonly role: TeamRole;
  readonly joinedAtUtc: string;
}

export interface TeamDetail {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  readonly myRole: TeamRole;
  /** Only present for admins and owners. */
  readonly joinCode: string | null;
  readonly createdAtUtc: string;
  readonly members: readonly TeamMember[];
}

export interface DayPlan {
  readonly date: IsoDate;
  readonly status: AttendanceStatus;
  readonly note: string | null;
  readonly updatedAtUtc: string;
}

export interface ScheduleRow {
  readonly member: TeamMember;
  /** Sparse: only the days this person has actually planned. */
  readonly days: readonly DayPlan[];
}

export interface DayTotals {
  readonly date: IsoDate;
  readonly isWeekend: boolean;
  readonly inOffice: number;
  readonly remote: number;
  readonly travelling: number;
  readonly away: number;
  readonly notPlanned: number;
}

export interface TeamSchedule {
  readonly teamId: string;
  readonly start: IsoDate;
  readonly end: IsoDate;
  readonly today: IsoDate;
  readonly days: readonly IsoDate[];
  readonly rows: readonly ScheduleRow[];
  readonly totals: readonly DayTotals[];
}

export interface DayRosterEntry {
  readonly member: TeamMember;
  readonly status: AttendanceStatus | null;
  readonly note: string | null;
}

export interface DayRoster {
  readonly teamId: string;
  readonly date: IsoDate;
  readonly totals: DayTotals;
  readonly entries: readonly DayRosterEntry[];
}

export interface DayPlanInput {
  readonly date: IsoDate;
  readonly status: AttendanceStatus;
  readonly note?: string | null;
}

/** RFC 9457 problem details, plus the machine-readable `code` the API adds. */
export interface ProblemDetails {
  readonly title?: string;
  readonly detail?: string;
  readonly status?: number;
  readonly code?: string;
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}
