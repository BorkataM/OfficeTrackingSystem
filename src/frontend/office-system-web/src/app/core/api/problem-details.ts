import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from './api.models';

const FALLBACK = 'Something went wrong. Please try again.';

/**
 * The API always answers failures with problem details, so error handling in the
 * UI reduces to reading them out. Anything else (a dropped connection, a proxy
 * error page) falls back to a plain sentence rather than leaking a stack trace.
 */
export function problemDetailsOf(error: unknown): ProblemDetails | null {
  if (!(error instanceof HttpErrorResponse)) {
    return null;
  }

  const body = error.error as unknown;

  if (body && typeof body === 'object' && ('detail' in body || 'title' in body || 'errors' in body)) {
    return body as ProblemDetails;
  }

  return null;
}

export function errorMessageOf(error: unknown): string {
  const problem = problemDetailsOf(error);

  if (problem?.errors) {
    const first = Object.values(problem.errors).flat()[0];

    if (first) {
      return first;
    }
  }

  if (problem?.detail) {
    return problem.detail;
  }

  if (error instanceof HttpErrorResponse && error.status === 0) {
    return 'Cannot reach the server. Is the API running?';
  }

  if (error instanceof HttpErrorResponse && error.status === 429) {
    return 'Too many attempts. Please wait a minute and try again.';
  }

  return problem?.title ?? FALLBACK;
}

/** Field-level messages keyed by camelCase property name, for inline form errors. */
export function fieldErrorsOf(error: unknown): Record<string, string> {
  const problem = problemDetailsOf(error);

  if (!problem?.errors) {
    return {};
  }

  return Object.fromEntries(
    Object.entries(problem.errors)
      .map(([field, messages]) => [field, messages[0]])
      .filter((entry): entry is [string, string] => entry[1] !== undefined),
  );
}

/** The API's stable machine-readable code, for reacting to a specific failure. */
export function errorCodeOf(error: unknown): string | null {
  return problemDetailsOf(error)?.code ?? null;
}
