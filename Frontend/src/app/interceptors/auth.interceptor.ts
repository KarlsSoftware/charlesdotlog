import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/**
 * Functional HTTP interceptor (Angular 19 style — no class needed).
 *
 * Automatically attaches the JWT token to every outgoing HTTP request.
 * This way, components and services don't need to manually add headers.
 *
 * .NET equivalent: middleware that modifies the request pipeline.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).getToken();

  if (token) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
    });
  }

  return next(req);
};
