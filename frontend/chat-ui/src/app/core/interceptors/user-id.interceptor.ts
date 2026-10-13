import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

/**
 * Adds the logged-in user's id to every request sent to our API,
 * so the server knows who is calling.
 */
export const userIdInterceptor: HttpInterceptorFn = (req, next) => {
  const user = inject(AuthService).currentUser();

  if (!user || !req.url.startsWith(environment.apiUrl)) {
    return next(req);
  }

  return next(req.clone({ setHeaders: { 'X-User-Id': user.id } }));
};
