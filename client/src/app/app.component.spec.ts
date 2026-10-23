import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { MsalBroadcastService, MsalService } from '@azure/msal-angular';
import { Subject } from 'rxjs';
import { App } from './app.component';

describe('App navigation', () => {
  function create(roles: string[] | undefined) {
    const account = roles ? { idTokenClaims: { roles } } : undefined;
    TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        {
          provide: MsalService,
          useValue: {
            instance: {
              getActiveAccount: () => account ?? null,
              getAllAccounts: () => (account ? [account] : [])
            }
          }
        },
        { provide: MsalBroadcastService, useValue: { msalSubject$: new Subject() } }
      ]
    });
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  it('shows the Order History link for Support users', () => {
    expect(create(['Support'])).toContain('Order History');
  });

  it('hides the Order History link for non-Support users', () => {
    expect(create(['Other'])).not.toContain('Order History');
  });

  it('hides the Order History link when signed out', () => {
    expect(create(undefined)).not.toContain('Order History');
  });
});
