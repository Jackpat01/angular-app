import { Routes } from '@angular/router';
import { BaseLayout } from './layouts/base-layout/base-layout';
import { BaseWithProjectLayout } from './layouts/base-with-project-layout/base-with-project-layout';
import { BaseWithMenuLayout } from './layouts/base-with-menu-layout/base-with-menu-layout';
import { Home } from './pages/home/home';
import { Login } from './pages/home/login/login';
import { ErrorPage } from './pages/home/error/error';
import { UserDashboard } from './pages/user-dashboard/user-dashboard';
import { AdminDashboard } from './pages/admin-dashboard/admin-dashboard';
import { AdminUsersComponent } from './pages/admin-users/admin-users';
import { AdminReportsComponent } from './pages/admin-reports/admin-reports';
import { SampleForm } from './pages/sample-form/sample-form';
import { AuthGuard } from './guards/auth.guard';
import {AcceptTerms} from './pages/home/accept-terms/accept-terms';

export const routes: Routes = [
  {
    path: '',
    component: BaseLayout,
    children: [
      {
        path: '',
        component: Home,
        children: [
          { path: '', redirectTo: 'login', pathMatch: 'full' },
          { path: 'login', component: Login },
          { path: 'accept', component: AcceptTerms },
          { path: 'error', component: ErrorPage }
        ]
      },
      { path: 'form', component: SampleForm }
    ]
  },
  {
    path: 'user-dashboard',
    component: BaseWithProjectLayout,
    canActivate: [AuthGuard],
    children: [
      { path: '', component: UserDashboard }
    ]
  },
  {
    path: 'admin',
    component: BaseWithMenuLayout,
    canActivate: [AuthGuard],
    children: [
      { path: '', component: AdminDashboard },
      { path: 'dashboard', component: AdminDashboard },
      { path: 'users', component: AdminUsersComponent },
      { path: 'reports', component: AdminReportsComponent }
    ]
  }
];
