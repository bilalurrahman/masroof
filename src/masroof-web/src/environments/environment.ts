export const environment = {
  production: true,
  apiBaseUrl: '/api',
  auth: {
    enabled: true,
    authority: 'http://localhost:8081/realms/masroof',
    clientId: 'masroof-spa',
    scope: 'openid profile',
    redirectUrl: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
};
