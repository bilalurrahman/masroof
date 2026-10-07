import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../ui/toast.service';

/**
 * Surfaces API errors as toasts using the RFC 9457 ProblemDetails `title`/`detail`.
 * 409 (duplicate) is handled by callers, so it is passed through quietly.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status !== 409) {
        toast.error(describe(err));
      }
      return throwError(() => err);
    }),
  );
};

function describe(err: HttpErrorResponse): string {
  if (err.status === 0) return 'Cannot reach the server.';
  const p = err.error;
  if (p && typeof p === 'object') {
    if (p.title && p.detail && p.title !== p.detail) return `${p.title}: ${p.detail}`;
    if (p.title) return p.title;
    if (p.detail) return p.detail;
  }
  return `Request failed (${err.status}).`;
}
