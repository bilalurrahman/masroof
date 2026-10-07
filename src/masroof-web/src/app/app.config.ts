import {
  ApplicationConfig,
  EnvironmentProviders,
  Provider,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors, HttpInterceptorFn } from '@angular/common/http';
import { LogLevel, authInterceptor, provideAuth } from 'angular-auth-oidc-client';
import { environment } from '../environments/environment';
import { routes } from './app.routes';
import { errorInterceptor } from './core/http/error.interceptor';

// OIDC is wired only when enabled (production / Keycloak). In dev the API uses Auth:DevBypass,
// so neither provideAuth nor the token interceptor are registered.
const authProviders: (Provider | EnvironmentProviders)[] = environment.auth.enabled
  ? [
      provideAuth({
        config: {
          authority: environment.auth.authority,
          redirectUrl: environment.auth.redirectUrl,
          postLogoutRedirectUri: environment.auth.postLogoutRedirectUri,
          clientId: environment.auth.clientId,
          scope: environment.auth.scope,
          responseType: 'code',
          silentRenew: true,
          useRefreshToken: true,
          renewTimeBeforeTokenExpiresInSeconds: 30,
          secureRoutes: [environment.apiBaseUrl],
          logLevel: environment.production ? LogLevel.Error : LogLevel.Warn,
        },
      }),
    ]
  : [];

const interceptors: HttpInterceptorFn[] = environment.auth.enabled
  ? [authInterceptor(), errorInterceptor]
  : [errorInterceptor];

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors(interceptors)),
    ...authProviders,
  ],
};
