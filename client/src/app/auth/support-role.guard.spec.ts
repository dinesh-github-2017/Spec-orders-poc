import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { MsalService } from '@azure/msal-angular';
import { isSupportUser, supportRoleGuard } from './support-role.guard';

describe('supportRoleGuard', () => {
  function configure(roles: unknown) {
    const account = roles === undefined ? undefined : { idTokenClaims: { roles } };
    const auth = {
      instance: {
        getActiveAccount: () => account ?? null,
        getAllAccounts: () => (account ? [account] : [])
      }
    };
    const tree = {} as UrlTree;
    TestBed.configureTestingModule({
      providers: [
        { provide: MsalService, useValue: auth },
        { provide: Router, useValue: { parseUrl: vi.fn().mockReturnValue(tree) } }
      ]
    });
    return { auth: auth as unknown as MsalService, tree };
  }

  it('allows an account whose roles contain Support', () => {
    configure(['Support']);

    expect(TestBed.runInInjectionContext(() => supportRoleGuard({} as never, {} as never))).toBe(true);
  });

  it('redirects an account without the Support role', () => {
    const { tree } = configure(['Other']);

    expect(TestBed.runInInjectionContext(() => supportRoleGuard({} as never, {} as never))).toBe(tree);
  });

  it('redirects when there is no account', () => {
    const { tree } = configure(undefined);

    expect(TestBed.runInInjectionContext(() => supportRoleGuard({} as never, {} as never))).toBe(tree);
  });

  it('isSupportUser is false when roles is not an array', () => {
    const { auth } = configure('Support');

    expect(isSupportUser(auth)).toBe(false);
  });
});
