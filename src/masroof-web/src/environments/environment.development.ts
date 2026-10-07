export const environment = {
  production: false,
  // `ng serve` proxies /api to the backend (see proxy.conf.json).
  apiBaseUrl: '/api',
  auth: {
    // Dev: the backend runs with Auth:DevBypass, so the SPA needs no token.
    enabled: false,
    authority: 'http://localhost:8081/realms/masroof',
    clientId: 'masroof-spa',
    scope: 'openid profile',
    redirectUrl: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
};
