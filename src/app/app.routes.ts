import { Routes } from '@angular/router';
import { PublicLayout } from './layouts/public-layout/public-layout';
import { DashboardLayout } from './layouts/dashboard-layout/dashboard-layout';
import { AdminLayout } from './layouts/admin-layout/admin-layout';
import { Home } from './pages/home/home';
import { Login } from './pages/login/login';
import { Error } from './pages/error/error';
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
      { path: '', component: Home },
      { path: 'login', component: Login },
      { path: 'error', component: Error },
      { path: 'form', component: SampleForm }
    ]
  },
  {
    path: 'user-dashboard',
    component: DashboardLayout,
    children: [
      { path: '', component: UserDashboard }
    ]
  },
  {
    path: 'admin',
    component: AdminLayout,
    children: [
      { path: '', component: AdminDashboard },
      { path: 'dashboard', component: AdminDashboard },
      { path: 'users', component: AdminUsersComponent },
      { path: 'reports', component: AdminReportsComponent }
    ]
  }
];
