import { inject } from '@angular/core';
import { Routes, Router, CanActivateFn } from '@angular/router';
import { map, take } from 'rxjs';
import { AuthService } from './core/services/auth.service';
import { RequestFormComponent } from './components/request-form/request-form.component';
import { MyRequestsComponent } from './components/my-requests/my-requests.component';
import { AdminDashboardComponent } from './components/admin-dashboard/admin-dashboard.component';

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  return authService.userSession$.pipe(
    take(1),
    map(user => user?.isAdmin ? true : router.parseUrl('/'))
  );
};

export const routes: Routes = [
  { path: 'request', component: RequestFormComponent },
  { path: 'my-requests', component: MyRequestsComponent },
  { 
    path: 'admin', 
    component: AdminDashboardComponent, 
    canActivate: [adminGuard]
  },
  { path: '', redirectTo: '/request', pathMatch: 'full' },
  { path: '**', redirectTo: '/request' }
];
