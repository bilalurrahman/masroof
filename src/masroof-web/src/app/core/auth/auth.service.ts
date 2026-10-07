import { Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { environment } from '../../../environments/environment';

/**
 * UI-facing auth facade. When OIDC is enabled it delegates to angular-auth-oidc-client and
 * mirrors its state into signals for the shell; in development (Auth:DevBypass on the API) OIDC
 * is not provided, so the user is reported authenticated and login/logout are no-ops. Token
 * attachment to /api is handled by the library's HTTP interceptor (secureRoutes), not here.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly oidc = inject(OidcSecurityService, { optional: true });
  readonly authEnabled = environment.auth.enabled;

  readonly isAuthenticated = signal<boolean>(!this.authEnabled);
  readonly userName = signal<string | null>(null);

  constructor() {
    if (!this.authEnabled || !this.oidc) return;

    this.oidc.isAuthenticated$
      .pipe(takeUntilDestroyed())
      .subscribe((r) => this.isAuthenticated.set(r.isAuthenticated));

    this.oidc.userData$
      .pipe(takeUntilDestroyed())
      .subscribe((r) => this.userName.set(r.userData?.name ?? r.userData?.preferred_username ?? null));
  }

  login(): void {
    this.oidc?.authorize();
  }

  logout(): void {
    this.oidc?.logoff().subscribe();
  }
}
