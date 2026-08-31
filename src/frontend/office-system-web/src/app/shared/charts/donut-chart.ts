import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

export interface DonutSlice {
  readonly key: string;
  readonly label: string;
  readonly value: number;
  /** A CSS colour, usually a chart token. */
  readonly color: string;
}

interface Arc extends DonutSlice {
  readonly share: number;
  readonly dashLength: number;
  readonly dashOffset: number;
}

const RADIUS = 60;
const STROKE = 22;
/** A 2px surface gap between neighbouring fills, expressed along the ring. */
const GAP = 2;

/**
 * Part-to-whole ring. Values are always readable from the legend, so the hover
 * readout in the hole enhances rather than gates.
 */
@Component({
  selector: 'app-donut-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="wrap">
      <svg [attr.viewBox]="'0 0 ' + size + ' ' + size" role="img" [attr.aria-label]="ariaLabel()">
        <g [attr.transform]="'rotate(-90 ' + center + ' ' + center + ')'">
          <circle
            class="track"
            [attr.cx]="center"
            [attr.cy]="center"
            [attr.r]="radius"
            [attr.stroke-width]="stroke"
            fill="none"
          />
          @for (arc of arcs(); track arc.key) {
            <circle
              class="arc"
              [class.dimmed]="hovered() !== null && hovered() !== arc.key"
              [attr.cx]="center"
              [attr.cy]="center"
              [attr.r]="radius"
              [attr.stroke]="arc.color"
              [attr.stroke-width]="stroke"
              [attr.stroke-dasharray]="arc.dashLength + ' ' + (circumference - arc.dashLength)"
              [attr.stroke-dashoffset]="-arc.dashOffset"
              fill="none"
              stroke-linecap="butt"
              (pointerenter)="hovered.set(arc.key)"
              (pointerleave)="hovered.set(null)"
            />
          }
        </g>

        <text class="value" [attr.x]="center" [attr.y]="center - 2" text-anchor="middle">{{ readoutValue() }}</text>
        <text class="caption" [attr.x]="center" [attr.y]="center + 16" text-anchor="middle">{{ readoutLabel() }}</text>
      </svg>
    </div>
  `,
  styles: `
    .wrap {
      display: flex;
      justify-content: center;
    }

    svg {
      width: 100%;
      max-width: 168px;
      height: auto;
      overflow: visible;
    }

    .track {
      stroke: var(--chart-track);
    }

    .arc {
      cursor: default;
      transition: opacity var(--duration-fast) var(--ease);
    }

    /* Hovering one slice recedes the others rather than moving anything. */
    .arc.dimmed {
      opacity: 0.28;
    }

    .value {
      fill: var(--text);
      font-size: 1.375rem;
      font-weight: 650;
      letter-spacing: -0.02em;
    }

    .caption {
      fill: var(--text-subtle);
      font-size: 0.6875rem;
      font-weight: 600;
    }
  `,
})
export class DonutChart {
  protected readonly radius = RADIUS;
  protected readonly stroke = STROKE;
  protected readonly size = (RADIUS + STROKE / 2) * 2;
  protected readonly center = this.size / 2;
  protected readonly circumference = 2 * Math.PI * RADIUS;

  readonly slices = input.required<readonly DonutSlice[]>();
  readonly totalLabel = input<string>('total');

  protected readonly hovered = signal<string | null>(null);

  protected readonly total = computed(() =>
    this.slices().reduce((sum, slice) => sum + slice.value, 0),
  );

  protected readonly arcs = computed<readonly Arc[]>(() => {
    const total = this.total();

    if (total <= 0) {
      return [];
    }

    const present = this.slices().filter((slice) => slice.value > 0);
    let offset = 0;

    return present.map((slice) => {
      const share = slice.value / total;
      const full = share * this.circumference;
      // Only inset a gap when there is a neighbour to separate from.
      const gap = present.length > 1 ? GAP : 0;
      const arc: Arc = { ...slice, share, dashLength: Math.max(1, full - gap), dashOffset: offset };

      offset += full;

      return arc;
    });
  });

  protected readonly readoutValue = computed(() => {
    const key = this.hovered();

    if (key === null) {
      return `${this.total()}`;
    }

    const slice = this.slices().find((candidate) => candidate.key === key);

    return slice ? `${Math.round((slice.value / Math.max(1, this.total())) * 100)}%` : `${this.total()}`;
  });

  protected readonly readoutLabel = computed(() => {
    const key = this.hovered();

    if (key === null) {
      return this.totalLabel();
    }

    return this.slices().find((candidate) => candidate.key === key)?.label ?? this.totalLabel();
  });

  protected readonly ariaLabel = computed(() =>
    this.slices()
      .filter((slice) => slice.value > 0)
      .map((slice) => `${slice.label}: ${slice.value}`)
      .join(', '),
  );
}
