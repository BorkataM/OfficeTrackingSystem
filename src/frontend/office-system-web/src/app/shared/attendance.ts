import { AttendanceStatus, TeamRole } from '../core/api/api.models';
import { IconName } from './icon/icon';

export interface StatusDescriptor {
  readonly status: AttendanceStatus;
  readonly label: string;
  readonly cssClass: string;
  readonly icon: IconName;
  readonly hint: string;
}

const DESCRIPTORS: Readonly<Record<AttendanceStatus, StatusDescriptor>> = {
  Office: {
    status: 'Office',
    label: 'In office',
    cssClass: 'status-office',
    icon: 'building',
    hint: 'At a desk in the office',
  },
  Remote: {
    status: 'Remote',
    label: 'Remote',
    cssClass: 'status-remote',
    icon: 'laptop',
    hint: 'Working, but not from the office',
  },
  Travelling: {
    status: 'Travelling',
    label: 'Vacation',
    cssClass: 'status-travelling',
    icon: 'plane',
    hint: 'Time off, out of office',
  },
  Away: {
    status: 'Away',
    label: 'Away',
    cssClass: 'status-away',
    icon: 'umbrella',
    hint: 'Sick leave or a public holiday',
  },
};

export const STATUS_DESCRIPTORS: readonly StatusDescriptor[] = Object.values(DESCRIPTORS);

export function describeStatus(status: AttendanceStatus): StatusDescriptor {
  return DESCRIPTORS[status];
}

export function canManageTeam(role: TeamRole): boolean {
  return role === 'Admin' || role === 'Owner';
}

/** Initials for the avatar: first letters of the first and last word. */
export function initialsOf(displayName: string): string {
  const words = displayName.trim().split(/\s+/).filter(Boolean);

  if (words.length === 0) {
    return '?';
  }

  const first = words[0]![0]!;
  const last = words.length > 1 ? words[words.length - 1]![0]! : '';

  return (first + last).toUpperCase();
}
