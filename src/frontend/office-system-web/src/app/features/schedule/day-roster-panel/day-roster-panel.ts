import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { DayRoster, DayRosterEntry, IsoDate } from '../../../core/api/api.models';
import { errorMessageOf } from '../../../core/api/problem-details';
import { ScheduleApi } from '../../../core/api/schedule.api';
import { describeRelativeDay, formatDayMonthLong, formatWeekdayLong } from '../../../core/dates';
import { LatestRequest } from '../../../core/latest-request';
import { STATUS_DESCRIPTORS } from '../../../shared/attendance';
import { Avatar } from '../../../shared/avatar/avatar';
import { Icon } from '../../../shared/icon/icon';

interface RosterGroup {
  readonly key: string;
  readonly label: string;
  readonly cssClass: string;
  readonly entries: readonly DayRosterEntry[];
}

/** "Who is in on this day", opened from a day header in the grid. */
@Component({
  selector: 'app-day-roster-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Avatar, Icon],
  template: `
    <div class="card">
      <header class="head">
        <div>
          <h2>{{ relativeLabel() ? relativeLabel() + ' · ' + weekdayLabel() : weekdayLabel() }}</h2>
          <p class="muted">{{ dateLabel() }}</p>
        </div>
        <button type="button" class="btn btn-icon btn-ghost" (click)="closed.emit()" aria-label="Close">
          <app-icon name="close" [size]="18" />
        </button>
      </header>

      @if (error(); as message) {
        <p class="form-error body"><app-icon name="alert" [size]="16" /><span>{{ message }}</span></p>
      } @else if (loading()) {
        <div class="body stack gap-3">
          @for (line of [0, 1, 2]; track line) {
            <div class="skeleton" style="height: 44px"></div>
          }
        </div>
      } @else if (roster(); as data) {
        <div class="summary">
          <div class="summary-tile">
            <span class="summary-value">{{ data.totals.inOffice }}</span>
            <span class="summary-label">in the office</span>
          </div>
          <div class="summary-tile">
            <span class="summary-value">{{ data.totals.remote }}</span>
            <span class="summary-label">remote</span>
          </div>
          <div class="summary-tile">
            <span class="summary-value">{{ data.totals.travelling }}</span>
            <span class="summary-label">travelling</span>
          </div>
          <div class="summary-tile">
            <span class="summary-value">{{ data.totals.away }}</span>
            <span class="summary-label">away</span>
          </div>
          <div class="summary-tile muted-tile">
            <span class="summary-value">{{ data.totals.notPlanned }}</span>
            <span class="summary-label">not planned</span>
          </div>
        </div>

        <div class="groups">
          @for (group of groups(); track group.key) {
            <section class="group">
              <h3 class="group-head">
                <span class="chip" [class]="group.cssClass"><span class="chip-dot"></span>{{ group.label }}</span>
                <span class="subtle text-xs">{{ group.entries.length }}</span>
              </h3>

              <ul class="people">
                @for (entry of group.entries; track entry.member.userId) {
                  <li class="person">
                    <app-avatar
                      [name]="entry.member.displayName"
                      [color]="entry.member.accentColor"
                      size="sm"
                    />
                    <span class="person-text">
                      <span class="person-name truncate">{{ entry.member.displayName }}</span>
                      @if (entry.note) {
                        <span class="person-note truncate">{{ entry.note }}</span>
                      }
                    </span>
                  </li>
                }
              </ul>
            </section>
          } @empty {
            <div class="empty">
              <span class="empty-icon"><app-icon name="calendar" [size]="22" /></span>
              <p>Nobody has planned this day yet.</p>
            </div>
          }
        </div>
      }
    </div>
  `,
  styleUrl: './day-roster-panel.scss',
})
export class DayRosterPanel {
  private readonly api = inject(ScheduleApi);

  readonly teamId = input.required<string>();
  readonly date = input.required<IsoDate>();
  readonly today = input.required<IsoDate>();

  readonly closed = output<void>();

  protected readonly roster = signal<DayRoster | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  private readonly request = new LatestRequest();

  protected readonly relativeLabel = computed(() => describeRelativeDay(this.date(), this.today()));
  protected readonly weekdayLabel = computed(() => formatWeekdayLong(this.date()));
  protected readonly dateLabel = computed(() => formatDayMonthLong(this.date()));

  /** Grouped by status, in the fixed order of the legend, empty groups dropped. */
  protected readonly groups = computed<readonly RosterGroup[]>(() => {
    const data = this.roster();

    if (!data) {
      return [];
    }

    const planned: RosterGroup[] = STATUS_DESCRIPTORS.map((descriptor) => ({
      key: descriptor.status,
      label: descriptor.label,
      cssClass: descriptor.cssClass,
      entries: data.entries.filter((entry) => entry.status === descriptor.status),
    }));

    const unplanned: RosterGroup = {
      key: 'Unplanned',
      label: 'Not planned',
      cssClass: 'status-unplanned',
      entries: data.entries.filter((entry) => entry.status === null),
    };

    return [...planned, unplanned].filter((group) => group.entries.length > 0);
  });

  constructor() {
    effect(() => {
      const teamId = this.teamId();
      const date = this.date();

      void this.load(teamId, date);
    });
  }

  private async load(teamId: string, date: IsoDate): Promise<void> {
    const token = this.request.begin();

    this.loading.set(true);
    this.error.set(null);

    try {
      const roster = await firstValueFrom(this.api.getDayRoster(teamId, date));

      // Clicking quickly along the day headers must not leave an earlier day's
      // roster on screen under a later day's heading.
      if (this.request.isCurrent(token)) {
        this.roster.set(roster);
      }
    } catch (error) {
      if (this.request.isCurrent(token)) {
        this.roster.set(null);
        this.error.set(errorMessageOf(error));
      }
    } finally {
      if (this.request.isCurrent(token)) {
        this.loading.set(false);
      }
    }
  }
}
