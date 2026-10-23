export const environment = {
  production: false,
  apiBaseUrl: 'https://localhost:7000',
  msal: {
    clientId: '<spa-client-id>',
    authority: 'https://login.microsoftonline.com/<tenant-id>',
    redirectUri: 'http://localhost:4200'
  },
  apiScope: 'api://<api-client-id>/access_as_user'
};
