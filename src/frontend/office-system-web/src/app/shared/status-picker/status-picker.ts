import { ChangeDetectionStrategy, Component, computed, input, linkedSignal, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AttendanceStatus, IsoDate } from '../../core/api/api.models';
import { describeRelativeDay, formatDayMonthLong, formatWeekdayLong } from '../../core/dates';
import { STATUS_DESCRIPTORS } from '../attendance';
import { Icon } from '../icon/icon';

export interface StatusChoice {
  readonly status: AttendanceStatus;
  readonly note: string | null;
}

/**
 * The one place a plan is edited. Used from both the week grid and the month view,
 * so the two can never offer different options.
 */
@Component({
  selector: 'app-status-picker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, Icon],
  template: `
    <div class="head">
      <div>
        <p class="day">{{ relativeLabel() ?? weekdayLabel() }}</p>
        <p class="date">{{ dateLabel() }}</p>
      </div>
      <button type="button" class="btn btn-icon btn-ghost" (click)="dismissed.emit()" aria-label="Close">
        <app-icon name="close" [size]="16" />
      </button>
    </div>

    <div class="options" role="group" aria-label="Attendance status">
      @for (option of statuses; track option.status) {
        <button
          type="button"
          class="option"
          [class]="option.cssClass"
          [class.selected]="option.status === current()"
          (click)="choose(option.status)"
        >
          <span class="chip-dot"></span>
          <span class="option-text">
            <span class="option-label">{{ option.label }}</span>
            <span class="option-hint">{{ option.hint }}</span>
          </span>
          @if (option.status === current()) {
            <app-icon name="check" [size]="16" />
          }
        </button>
      }
    </div>

    <div class="note">
      <label class="label" [attr.for]="noteId">Note <span class="subtle">(optional)</span></label>
      <input
        [id]="noteId"
        class="input"
        type="text"
        [(ngModel)]="noteDraft"
        maxlength="200"
        placeholder="Half day, customer visit, …"
        (keydown.enter)="commitNote()"
      />
    </div>

    <div class="actions">
      @if (current()) {
        <button type="button" class="btn btn-sm btn-danger" (click)="cleared.emit()">
          <app-icon name="trash" [size]="14" />
          Clear day
        </button>
      }
      <span class="grow"></span>
      <button type="button" class="btn btn-sm btn-primary" [disabled]="!current()" (click)="commitNote()">
        Save note
      </button>
    </div>
  `,
  styleUrl: './status-picker.scss',
})
export class StatusPicker {
  protected readonly statuses = STATUS_DESCRIPTORS;
  protected readonly noteId = `note-${Math.random().toString(36).slice(2, 9)}`;

  readonly date = input.required<IsoDate>();
  readonly today = input.required<IsoDate>();
  readonly current = input<AttendanceStatus | null>(null);
  readonly note = input<string | null>(null);

  readonly picked = output<StatusChoice>();
  readonly cleared = output<void>();
  readonly dismissed = output<void>();

  /** Seeded from the stored note, and re-seeded if the picker is pointed at another day. */
  protected readonly noteDraft = linkedSignal(() => this.note() ?? '');

  protected readonly relativeLabel = computed(() => describeRelativeDay(this.date(), this.today()));
  protected readonly weekdayLabel = computed(() => formatWeekdayLong(this.date()));
  protected readonly dateLabel = computed(() => formatDayMonthLong(this.date()));

  protected choose(status: AttendanceStatus): void {
    this.picked.emit({ status, note: trimmedOrNull(this.noteDraft()) });
  }

  protected commitNote(): void {
    const status = this.current();

    if (status) {
      this.picked.emit({ status, note: trimmedOrNull(this.noteDraft()) });
    }
  }
}

function trimmedOrNull(value: string): string | null {
  const trimmed = value.trim();

  return trimmed.length > 0 ? trimmed : null;
}
