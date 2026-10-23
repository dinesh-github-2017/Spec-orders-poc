import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { MsalBroadcastService, MsalService } from '@azure/msal-angular';
import { EventMessage, EventType } from '@azure/msal-browser';
import { filter } from 'rxjs';
import { isSupportUser } from './auth/support-role.guard';

@Component({
  imports: [RouterOutlet, RouterLink],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  private readonly auth = inject(MsalService);
  private readonly broadcast = inject(MsalBroadcastService);

  readonly isSupport = signal(false);

  constructor() {
    this.refresh();
    this.broadcast.msalSubject$
      .pipe(
        filter(
          (m: EventMessage) =>
            m.eventType === EventType.LOGIN_SUCCESS ||
            m.eventType === EventType.ACQUIRE_TOKEN_SUCCESS ||
            m.eventType === EventType.LOGOUT_SUCCESS
        )
      )
      .subscribe(() => this.refresh());
  }

  private refresh(): void {
    this.isSupport.set(isSupportUser(this.auth));
  }
}
