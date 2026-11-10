import { Routes } from '@angular/router';
import { PublicLayout } from './layouts/public-layout/public-layout';
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

export const routes: Routes = [
  {
    path: '',
    component: PublicLayout,
    children: [
      {
        path: '',
        component: Home,
        children: [
          { path: 'login', component: Login },
          { path: 'error', component: ErrorPage }
        ]
      },
      { path: 'form', component: SampleForm }
    ]
  },
  {
    path: 'user-dashboard',
    component: BaseWithProjectLayout,
    children: [
      { path: '', component: UserDashboard }
    ]
  },
  {
    path: 'admin',
    component: BaseWithMenuLayout,
    children: [
      { path: '', component: AdminDashboard },
      { path: 'dashboard', component: AdminDashboard },
      { path: 'users', component: AdminUsersComponent },
      { path: 'reports', component: AdminReportsComponent }
    ]
  }
];
