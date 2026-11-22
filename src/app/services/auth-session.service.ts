import { Inject, Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export type WorkspaceRole = 'Customer' | 'StoreManager' | 'Admin';
export interface AuthResponse {
  token: string;
  role: WorkspaceRole;
  expiresAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  readonly token = signal<string | null>(null);
  readonly role = signal<WorkspaceRole | null>(null);
  readonly userId = signal<number | null>(null);
  readonly expiresAt = signal<string | null>(null);
  readonly isAuthenticated = signal(false);

  constructor(@Inject(PLATFORM_ID) private readonly platformId: object) {
    this.restore();
  }

  accept(response: AuthResponse): void {
    const userId = this.readUserId(response.token);
    if (!userId) throw new Error('The API returned a token without a user identity.');
    this.token.set(response.token);
    this.role.set(response.role);
    this.userId.set(userId);
    this.expiresAt.set(response.expiresAtUtc);
    this.isAuthenticated.set(new Date(response.expiresAtUtc).getTime() > Date.now());
    if (isPlatformBrowser(this.platformId)) {
      sessionStorage.setItem(
        'bizspace.session',
        JSON.stringify({
          token: response.token,
          role: response.role,
          expiresAtUtc: response.expiresAtUtc,
        }),
      );
    }
  }

  clear(): void {
    this.token.set(null);
    this.role.set(null);
    this.userId.set(null);
    this.expiresAt.set(null);
    this.isAuthenticated.set(false);
    if (isPlatformBrowser(this.platformId)) sessionStorage.removeItem('bizspace.session');
  }

  private restore(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      const raw = sessionStorage.getItem('bizspace.session');
      if (!raw) return;
      const session = JSON.parse(raw) as AuthResponse;
      if (
        !session.token ||
        !session.role ||
        new Date(session.expiresAtUtc).getTime() <= Date.now()
      ) {
        this.clear();
        return;
      }
      const userId = this.readUserId(session.token);
      if (!userId) {
        this.clear();
        return;
      }
      this.token.set(session.token);
      this.role.set(session.role);
      this.userId.set(userId);
      this.expiresAt.set(session.expiresAtUtc);
      this.isAuthenticated.set(true);
    } catch {
      this.clear();
    }
  }

  private readUserId(token: string): number | null {
    try {
      const payload = token.split('.')[1]?.replace(/-/g, '+').replace(/_/g, '/');
      if (!payload) return null;
      const padded = payload.padEnd(payload.length + ((4 - (payload.length % 4)) % 4), '=');
      const claims = JSON.parse(atob(padded)) as Record<string, unknown>;
      const value =
        claims['nameid'] ??
        claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ??
        claims['sub'];
      const id = Number(value);
      return Number.isInteger(id) && id > 0 ? id : null;
    } catch {
      return null;
    }
  }
}

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const session = inject(AuthSessionService);
  const token = session.token();
  const authenticatedRequest = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;
  return next(authenticatedRequest).pipe(
    catchError((error) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        token &&
        !request.url.includes('/auth/')
      )
        session.clear();
      return throwError(() => error);
    }),
  );
};
