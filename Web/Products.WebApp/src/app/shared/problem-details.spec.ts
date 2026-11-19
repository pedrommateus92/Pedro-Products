import { HttpErrorResponse } from '@angular/common/http';
import { errorMessage, hasStatus, validationErrors } from './problem-details';

function httpError(status: number, error: unknown = null): HttpErrorResponse {
  return new HttpErrorResponse({ status, error });
}

describe('problem details helpers', () => {
  it('uses the detail of a problem response', () => {
    const error = httpError(409, { title: 'Conflict', detail: 'Insufficient stock.' });

    expect(errorMessage(error)).toBe('Insufficient stock.');
  });

  it('falls back to the title when there is no detail', () => {
    expect(errorMessage(httpError(404, { title: 'Not Found' }))).toBe('Not Found');
  });

  it('uses the fallback for errors without a problem body', () => {
    expect(errorMessage(httpError(500, 'boom'), 'Could not load.')).toBe('Could not load.');
    expect(errorMessage(new Error('not http'), 'Could not load.')).toBe('Could not load.');
  });

  it.each([0, 502, 503, 504])('reports an unreachable server for status %i', (status) => {
    expect(errorMessage(httpError(status))).toBe(
      'Cannot reach the server. Check that the API is running.',
    );
  });

  it('returns field errors from a validation response', () => {
    const error = httpError(400, { errors: { name: ["'Name' must not be empty."] } });

    expect(validationErrors(error)).toEqual({ name: ["'Name' must not be empty."] });
    expect(validationErrors(httpError(500))).toEqual({});
  });

  it('checks the HTTP status', () => {
    expect(hasStatus(httpError(409), 409)).toBe(true);
    expect(hasStatus(httpError(400), 409)).toBe(false);
    expect(hasStatus(new Error('x'), 409)).toBe(false);
  });
});
