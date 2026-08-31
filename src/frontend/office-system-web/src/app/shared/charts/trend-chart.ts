import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

export interface TrendPoint {
  readonly key: string;
  readonly label: string;
  readonly value: number;
}

interface PlottedPoint extends TrendPoint {
  readonly x: number;
  readonly y: number;
}

const WIDTH = 720;
const HEIGHT = 168;
const PADDING = { top: 14, right: 8, bottom: 22, left: 8 };

/**
 * Area + line for a single series over time. Too many points to label them all, so
 * the extremes are direct-labelled and a crosshair carries the rest — the pattern
 * the guidance calls for rather than a number on every point.
 */
@Component({
  selector: 'app-trend-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="head">
      <span class="readout">
        <strong>{{ activeValue() }}</strong>
        <span class="muted">{{ activeLabel() }}</span>
      </span>
    </div>

    <svg
      [attr.viewBox]="'0 0 ' + width + ' ' + height"
      preserveAspectRatio="none"
      role="img"
      [attr.aria-label]="ariaLabel()"
      (pointerleave)="hoveredKey.set(null)"
    >
      <!-- Hairline grid, solid and one shade off the surface. -->
      @for (line of gridLines(); track line.value) {
        <line class="grid" [attr.x1]="0" [attr.x2]="width" [attr.y1]="line.y" [attr.y2]="line.y" />
      }

      @if (points().length > 1) {
        <path class="area" [attr.d]="areaPath()" />
        <path class="line" [attr.d]="linePath()" />
      }

      @for (marker of markers(); track marker.key) {
        <circle class="marker" [attr.cx]="marker.x" [attr.cy]="marker.y" r="4.5" />
      }

      @if (hovered(); as point) {
        <line class="crosshair" [attr.x1]="point.x" [attr.x2]="point.x" [attr.y1]="0" [attr.y2]="plotBottom" />
        <circle class="cursor" [attr.cx]="point.x" [attr.cy]="point.y" r="5" />
      }

      <!-- Wide invisible hit bands, so the target is far bigger than the mark. -->
      @for (point of points(); track point.key) {
        <rect
          class="hit"
          [attr.x]="point.x - bandWidth() / 2"
          [attr.y]="0"
          [attr.width]="bandWidth()"
          [attr.height]="height"
          (pointerenter)="hoveredKey.set(point.key)"
        />
      }
    </svg>

    <div class="axis">
      <span>{{ firstLabel() }}</span>
      <span>{{ lastLabel() }}</span>
    </div>
  `,
  styles: `
    :host {
      display: block;
    }

    .head {
      display: flex;
      justify-content: flex-end;
      min-height: 20px;
      margin-bottom: var(--space-1);
    }

    .readout {
      display: inline-flex;
      align-items: baseline;
      gap: var(--space-2);
      font-size: var(--text-sm);
    }

    .readout strong {
      font-size: var(--text-base);
      font-weight: 650;
      font-variant-numeric: tabular-nums;
    }

    svg {
      display: block;
      width: 100%;
      height: 168px;
      overflow: visible;
    }

    .grid {
      stroke: var(--chart-grid);
      stroke-width: 1;
      vector-effect: non-scaling-stroke;
    }

    .area {
      fill: var(--chart-office);
      opacity: 0.14;
    }

    .line {
      fill: none;
      stroke: var(--chart-office);
      stroke-width: 2;
      stroke-linejoin: round;
      stroke-linecap: round;
      vector-effect: non-scaling-stroke;
    }

    .marker {
      fill: var(--chart-office);
      /* 2px surface ring keeps the marker legible where it overlaps the line. */
      stroke: var(--surface);
      stroke-width: 2;
      vector-effect: non-scaling-stroke;
    }

    .crosshair {
      stroke: var(--chart-axis);
      stroke-width: 1;
      vector-effect: non-scaling-stroke;
    }

    .cursor {
      fill: var(--surface);
      stroke: var(--chart-office);
      stroke-width: 2;
      vector-effect: non-scaling-stroke;
    }

    .hit {
      fill: transparent;
    }

    .axis {
      display: flex;
      justify-content: space-between;
      margin-top: var(--space-1);
      color: var(--text-subtle);
      font-size: var(--text-xs);
      font-variant-numeric: tabular-nums;
    }
  `,
})
export class TrendChart {
  protected readonly width = WIDTH;
  protected readonly height = HEIGHT;
  protected readonly plotBottom = HEIGHT - PADDING.bottom;

  readonly points_ = input.required<readonly TrendPoint[]>({ alias: 'points' });
  readonly unitLabel = input<string>('');

  protected readonly hoveredKey = signal<string | null>(null);

  private readonly max = computed(() => Math.max(1, ...this.points_().map((point) => point.value)));

  protected readonly points = computed<readonly PlottedPoint[]>(() => {
    const source = this.points_();
    const max = this.max();
    const plotHeight = HEIGHT - PADDING.top - PADDING.bottom;
    const usable = WIDTH - PADDING.left - PADDING.right;
    const step = source.length > 1 ? usable / (source.length - 1) : 0;

    return source.map((point, index) => ({
      ...point,
      x: PADDING.left + index * step,
      y: PADDING.top + plotHeight - (point.value / max) * plotHeight,
    }));
  });

  protected readonly bandWidth = computed(() => {
    const count = this.points().length;

    return count === 0 ? WIDTH : WIDTH / count;
  });

  protected readonly linePath = computed(() =>
    this.points()
      .map((point, index) => `${index === 0 ? 'M' : 'L'}${point.x.toFixed(2)} ${point.y.toFixed(2)}`)
      .join(' '),
  );

  protected readonly areaPath = computed(() => {
    const points = this.points();

    if (points.length < 2) {
      return '';
    }

    const first = points[0]!;
    const last = points[points.length - 1]!;

    return `${this.linePath()} L${last.x.toFixed(2)} ${this.plotBottom} L${first.x.toFixed(2)} ${this.plotBottom} Z`;
  });

  protected readonly gridLines = computed(() => {
    const max = this.max();
    const plotHeight = HEIGHT - PADDING.top - PADDING.bottom;

    return [0, 0.5, 1].map((fraction) => ({
      value: Math.round(max * fraction),
      y: PADDING.top + plotHeight - fraction * plotHeight,
    }));
  });

  /** The extremes only: the two points worth naming without hovering. */
  protected readonly markers = computed<readonly PlottedPoint[]>(() => {
    const points = this.points();

    if (points.length === 0) {
      return [];
    }

    const max = Math.max(...points.map((point) => point.value));
    const peak = points.find((point) => point.value === max);

    return peak ? [peak] : [];
  });

  protected readonly hovered = computed<PlottedPoint | null>(() => {
    const key = this.hoveredKey();

    return key === null ? null : (this.points().find((point) => point.key === key) ?? null);
  });

  protected readonly activeValue = computed(() => {
    const point = this.hovered() ?? this.markers()[0] ?? null;

    return point === null ? '—' : `${point.value}`;
  });

  protected readonly activeLabel = computed(() => {
    const hovered = this.hovered();

    if (hovered) {
      return `${this.unitLabel()} on ${hovered.label}`.trim();
    }

    const peak = this.markers()[0];

    return peak ? `${this.unitLabel()} at the busiest, ${peak.label}`.trim() : '';
  });

  protected readonly firstLabel = computed(() => this.points()[0]?.label ?? '');

  protected readonly lastLabel = computed(() => {
    const points = this.points();

    return points.length > 1 ? (points[points.length - 1]?.label ?? '') : '';
  });

  protected readonly ariaLabel = computed(
    () => `${this.unitLabel()} from ${this.firstLabel()} to ${this.lastLabel()}`,
  );
}
