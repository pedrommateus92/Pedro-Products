import { HttpErrorResponse } from '@angular/common/http';

/** RFC 9457 problem details, as returned by the API for every error. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

function asProblem(error: unknown): ProblemDetails | null {
  if (error instanceof HttpErrorResponse && error.error && typeof error.error === 'object') {
    return error.error as ProblemDetails;
  }
  return null;
}

/** A short, human-readable message for any HTTP error. */
export function errorMessage(
  error: unknown,
  fallback = 'Something went wrong. Please try again.',
): string {
  // 0: the browser got no response; 502-504: the dev proxy could not reach the API.
  if (error instanceof HttpErrorResponse && [0, 502, 503, 504].includes(error.status)) {
    return 'Cannot reach the server. Check that the API is running.';
  }
  const problem = asProblem(error);
  return problem?.detail ?? problem?.title ?? fallback;
}

/** Field errors from a 400 validation response, keyed by camelCase property name. */
export function validationErrors(error: unknown): Record<string, string[]> {
  return asProblem(error)?.errors ?? {};
}

export function hasStatus(error: unknown, status: number): boolean {
  return error instanceof HttpErrorResponse && error.status === status;
}
