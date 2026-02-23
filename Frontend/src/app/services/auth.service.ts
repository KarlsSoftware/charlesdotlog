import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * Manages admin authentication — login, logout, token storage.
 *
 * Uses sessionStorage (not localStorage) so the token is automatically
 * cleared when the browser tab is closed — safer for an admin panel.
 *
 * Uses Angular Signals for reactive auth state — components can read
 * `isLoggedIn()` and Angular re-renders automatically when it changes.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private tokenKey = 'admin_token';

  /** Reactive signal — true when a token exists in sessionStorage */
  isLoggedIn = signal(!!sessionStorage.getItem(this.tokenKey));

  constructor(private http: HttpClient) {}

  /** POST password to backend, store returned JWT token */
  login(password: string) {
    return this.http
      .post<{ token: string }>(`${environment.apiUrl}/api/auth/login`, { password })
      .pipe(
        tap((res) => {
          sessionStorage.setItem(this.tokenKey, res.token);
          this.isLoggedIn.set(true);
        })
      );
  }

  /** Remove token and update signal */
  logout() {
    sessionStorage.removeItem(this.tokenKey);
    this.isLoggedIn.set(false);
  }

  /** Read raw token (used by the HTTP interceptor) */
  getToken(): string | null {
    return sessionStorage.getItem(this.tokenKey);
  }
}
