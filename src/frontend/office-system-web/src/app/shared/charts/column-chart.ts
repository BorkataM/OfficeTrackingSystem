import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export interface Column {
  readonly label: string;
  readonly value: number;
  /** Shown above the bar. Pre-formatted so the chart does no rounding of its own. */
  readonly display: string;
}

/**
 * Single-series columns with a direct label on every bar. One series means one
 * colour and no legend — the card title names it.
 */
@Component({
  selector: 'app-column-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="chart" role="img" [attr.aria-label]="ariaLabel()">
      @for (column of columns(); track column.label) {
        <div class="column" [class.best]="column.label === bestLabel()">
          <span class="value">{{ column.display }}</span>
          <div class="track">
            <div class="bar" [style.height.%]="heightOf(column)"></div>
          </div>
          <span class="label">{{ column.label }}</span>
        </div>
      }
    </div>
  `,
  styles: `
    .chart {
      display: grid;
      grid-auto-flow: column;
      grid-auto-columns: 1fr;
      /* 2px of surface between adjacent fills. */
      gap: var(--space-2);
      align-items: end;
    }

    .column {
      display: grid;
      grid-template-rows: auto 1fr auto;
      gap: var(--space-2);
      justify-items: center;
      height: 156px;
    }

    .value {
      color: var(--text-subtle);
      font-size: var(--text-xs);
      font-weight: 650;
      font-variant-numeric: tabular-nums;
    }

    .column.best .value {
      color: var(--text);
    }

    .track {
      display: flex;
      align-items: flex-end;
      width: 100%;
      max-width: 46px;
      height: 100%;
      background: var(--chart-track);
      border-radius: var(--radius-xs);
      overflow: hidden;
    }

    .bar {
      width: 100%;
      min-height: 2px;
      /* Rounded data-end, square against the baseline. */
      border-radius: 4px 4px 0 0;
      background: var(--chart-office);
      opacity: 0.55;
      transition: height var(--duration-slow) var(--ease), opacity var(--duration-fast) var(--ease);
    }

    /* Emphasis: the best day carries full weight, the rest are context. */
    .column.best .bar {
      opacity: 1;
    }

    .column:hover .bar {
      opacity: 1;
    }

    .label {
      color: var(--text-subtle);
      font-size: var(--text-xs);
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .column.best .label {
      color: var(--text);
    }
  `,
})
export class ColumnChart {
  readonly columns = input.required<readonly Column[]>();
  readonly unitLabel = input<string>('');

  private readonly max = computed(() => Math.max(0, ...this.columns().map((column) => column.value)));

  protected readonly bestLabel = computed(() => {
    const max = this.max();

    return max <= 0 ? null : (this.columns().find((column) => column.value === max)?.label ?? null);
  });

  protected heightOf(column: Column): number {
    const max = this.max();

    return max <= 0 ? 0 : (column.value / max) * 100;
  }

  protected readonly ariaLabel = computed(() =>
    this.columns()
      .map((column) => `${column.label}: ${column.display} ${this.unitLabel()}`.trim())
      .join(', '),
  );
}
