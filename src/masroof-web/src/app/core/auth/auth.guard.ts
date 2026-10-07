import { CanActivateFn } from '@angular/router';
import { autoLoginPartialRoutesGuard } from 'angular-auth-oidc-client';
import { environment } from '../../../environments/environment';

/**
 * Protects feature routes. In dev (OIDC disabled, API uses DevBypass) it lets everything
 * through. In production it defers to angular-auth-oidc-client, which redirects unauthenticated
 * users to Keycloak and restores the requested route after login.
 */
export const authGuard: CanActivateFn = (route, state) => {
  if (!environment.auth.enabled) return true;
  return autoLoginPartialRoutesGuard(route, state);
};
