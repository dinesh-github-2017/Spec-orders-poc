import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { MsalService } from '@azure/msal-angular';

export const SUPPORT_ROLE = 'Support';

export function isSupportUser(auth: MsalService): boolean {
  const account = auth.instance.getActiveAccount() ?? auth.instance.getAllAccounts()[0];
  const roles = account?.idTokenClaims?.roles;
  return Array.isArray(roles) && roles.includes(SUPPORT_ROLE);
}

export const supportRoleGuard: CanActivateFn = () => {
  const auth = inject(MsalService);
  const router = inject(Router);
  return isSupportUser(auth) ? true : router.parseUrl('/');
};
