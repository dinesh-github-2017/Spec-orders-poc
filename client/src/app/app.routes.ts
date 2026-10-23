import { Routes } from '@angular/router';
import { MsalGuard } from '@azure/msal-angular';
import { OrderHistoryComponent } from './order-history/order-history.component';
import { supportRoleGuard } from './auth/support-role.guard';

export const routes: Routes = [
  { path: 'order-history', component: OrderHistoryComponent, canActivate: [MsalGuard, supportRoleGuard] }
];
