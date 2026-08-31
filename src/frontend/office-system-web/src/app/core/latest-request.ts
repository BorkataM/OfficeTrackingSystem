/**
 * Guards against out-of-order responses: only the most recently started request may
 * apply its result.
 *
 * Without this, navigating while a request is still in flight lets the older
 * response land last and win — the week header moves but the grid snaps back to the
 * week the user just left.
 */
export class LatestRequest {
  private sequence = 0;

  /** Starts a request and returns the token it must present to apply its result. */
  begin(): number {
    this.sequence += 1;

    return this.sequence;
  }

  isCurrent(token: number): boolean {
    return token === this.sequence;
  }
}
