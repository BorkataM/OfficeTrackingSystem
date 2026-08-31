import { DestroyRef, Directive, ElementRef, afterRenderEffect, inject, input } from '@angular/core';

/** Space kept between the popover and the edge of the viewport. */
const VIEWPORT_MARGIN = 12;

/** Space between the anchor and the popover. */
const ANCHOR_GAP = 6;

/** Below this, a popover is too cramped to be worth showing on its preferred side. */
const MIN_USABLE_HEIGHT = 180;

/**
 * Pins the host to an anchor element using fixed positioning.
 *
 * Fixed positioning is the whole point: a popover rendered inside the schedule grid
 * is clipped by the grid's own scroll container, and reaching the rest of it means
 * scrolling the entire table. Taking the popover out of that container lets it
 * overhang the table, and its own `overflow-y` means only the popover scrolls.
 *
 * The host is re-placed whenever the anchor changes, and on scroll and resize. If
 * the anchor leaves the viewport entirely it is hidden rather than clamped, so it
 * never floats next to nothing.
 */
@Directive({ selector: '[appAnchoredTo]' })
export class AnchoredTo {
  private readonly element: HTMLElement = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private readonly destroyRef = inject(DestroyRef);

  readonly anchor = input.required<HTMLElement>({ alias: 'appAnchoredTo' });

  constructor() {
    const style = this.element.style;
    style.position = 'fixed';
    style.zIndex = '60';
    style.overflowY = 'auto';
    // Keep a trackpad fling inside the popover instead of handing it to the page.
    style.overscrollBehavior = 'contain';

    const place = () => this.place();

    // Angular reuses this instance when the popover moves from one cell to the next,
    // so the anchor input changing is the only signal that it must be re-placed.
    //
    // This has to be an after-render effect. A plain effect can run while the
    // projected content is still going up, and measuring a half-built popover picks
    // the wrong side to open on. Deferring to requestAnimationFrame instead would
    // leave it unplaced in any tab that is not painting frames.
    afterRenderEffect(() => {
      this.anchor();
      this.place();
    });

    window.addEventListener('scroll', place, { capture: true, passive: true });
    window.addEventListener('resize', place, { passive: true });

    // The popover grows when a longer note or a second line of text appears.
    const resizeObserver = new ResizeObserver(place);
    resizeObserver.observe(this.element);

    this.destroyRef.onDestroy(() => {
      window.removeEventListener('scroll', place, { capture: true });
      window.removeEventListener('resize', place);
      resizeObserver.disconnect();
    });
  }

  private place(): void {
    const anchor = this.anchor().getBoundingClientRect();

    if (!isOnScreen(anchor)) {
      this.write('visibility', 'hidden');
      return;
    }

    this.write('visibility', 'visible');

    const viewportHeight = window.innerHeight;
    const viewportWidth = window.innerWidth;

    // scrollHeight reports the full content height even once max-height clamps it.
    const contentHeight = this.element.scrollHeight;
    const width = this.element.offsetWidth;

    const spaceBelow = viewportHeight - anchor.bottom - ANCHOR_GAP - VIEWPORT_MARGIN;
    const spaceAbove = anchor.top - ANCHOR_GAP - VIEWPORT_MARGIN;

    // Prefer below, and flip up only when that actually gains room.
    const openBelow = spaceBelow >= contentHeight || spaceBelow >= spaceAbove;

    const maxHeight = clamp(
      Math.max(openBelow ? spaceBelow : spaceAbove, MIN_USABLE_HEIGHT),
      0,
      viewportHeight - 2 * VIEWPORT_MARGIN,
    );

    const shownHeight = Math.min(contentHeight, maxHeight);

    const top = clamp(
      openBelow ? anchor.bottom + ANCHOR_GAP : anchor.top - ANCHOR_GAP - shownHeight,
      VIEWPORT_MARGIN,
      Math.max(VIEWPORT_MARGIN, viewportHeight - shownHeight - VIEWPORT_MARGIN),
    );

    const left = clamp(
      anchor.left,
      VIEWPORT_MARGIN,
      Math.max(VIEWPORT_MARGIN, viewportWidth - width - VIEWPORT_MARGIN),
    );

    this.write('left', `${Math.round(left)}px`);
    this.write('top', `${Math.round(top)}px`);
    this.write('maxHeight', `${Math.round(maxHeight)}px`);
  }

  /**
   * Only touches the style when the value actually changes. Writing unconditionally
   * from inside a ResizeObserver callback is how you get the "loop completed with
   * undelivered notifications" warning.
   */
  private write(property: 'left' | 'top' | 'maxHeight' | 'visibility', value: string): void {
    if (this.element.style[property] !== value) {
      this.element.style[property] = value;
    }
  }
}

function isOnScreen(rect: DOMRect): boolean {
  return (
    rect.bottom > 0 && rect.top < window.innerHeight && rect.right > 0 && rect.left < window.innerWidth
  );
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}
