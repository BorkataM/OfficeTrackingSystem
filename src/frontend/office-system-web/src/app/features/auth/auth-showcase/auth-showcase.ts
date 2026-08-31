import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Icon } from '../../../shared/icon/icon';

interface PreviewRow {
  readonly name: string;
  readonly color: string;
  readonly cells: readonly string[];
}

/** The marketing panel shared by the sign-in and sign-up screens. */
@Component({
  selector: 'app-auth-showcase',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon],
  template: `
    <div class="brand">
      <span class="brand-mark">
        <app-icon name="calendar" [size]="20" />
      </span>
      Office Days
    </div>

    <div class="pitch">
      <h1>Know who is in, before you come in.</h1>
      <p>
        Pick a team, mark your days, and see the week at a glance. No thread to scroll,
        no spreadsheet to maintain.
      </p>

      <div class="preview" aria-hidden="true">
        <div class="preview-head">
          <span>Platform</span>
          <span>Mon</span>
          <span>Tue</span>
          <span>Wed</span>
          <span>Thu</span>
          <span>Fri</span>
        </div>

        @for (row of rows; track row.name) {
          <div class="preview-row">
            <span class="preview-name">
              <i class="preview-dot" [style.background]="row.color"></i>
              {{ row.name }}
            </span>
            @for (cell of row.cells; track $index) {
              <span class="preview-cell" [class]="cell"></span>
            }
          </div>
        }
      </div>

      <div class="legend">
        <span><i style="background: #34d399"></i> In office</span>
        <span><i style="background: #818cf8"></i> Remote</span>
        <span><i style="background: #fbbf24"></i> Travelling</span>
        <span><i style="background: #94a3b8"></i> Away</span>
      </div>
    </div>

    <p class="foot">Built with .NET&nbsp;10 and Angular&nbsp;20.</p>
  `,
  styleUrl: './auth-showcase.scss',
})
export class AuthShowcase {
  protected readonly rows: readonly PreviewRow[] = [
    { name: 'Ada', color: '#f97316', cells: ['office', 'office', 'remote', 'office', 'remote'] },
    { name: 'Grace', color: '#ec4899', cells: ['remote', 'office', 'office', 'remote', 'away'] },
    { name: 'Alan', color: '#a855f7', cells: ['office', 'travel', 'travel', 'office', 'office'] },
    { name: 'Katherine', color: '#ef4444', cells: ['away', 'away', 'office', 'office', 'remote'] },
  ];
}
