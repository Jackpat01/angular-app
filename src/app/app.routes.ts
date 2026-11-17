import { Routes } from '@angular/router';
import { BaseLayout } from './layouts/base-layout/base-layout';
import { BaseWithProjectLayout } from './layouts/base-with-project-layout/base-with-project-layout';
import { BaseWithMenuLayout } from './layouts/base-with-menu-layout/base-with-menu-layout';
import { Home } from './pages/home/home';
import { Login } from './pages/home/login/login';
import { ErrorPage } from './pages/home/error/error';
import { AuthGuard } from './guards/auth.guard';
import { AcceptTerms } from './pages/home/accept-terms/accept-terms';
import { Projects } from './pages/projects/projects';
import { SetProjectComponent } from './pages/projects/set-project/set-project';
import { ProjectSelectionGuard } from './guards/project-selection.guard';

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
          { path: 'error', component: ErrorPage },
        ],
      },
    ],
  },
  {
    path: '',
    component: BaseWithProjectLayout,
    canActivate: [AuthGuard],
    children: [
      {
        path: 'projects',
        component: Projects,
        data: { bodyClass: 'layout-projects' },
      },
      {
        path: 'projects/set-project',
        component: SetProjectComponent,
        canActivate: [ProjectSelectionGuard],
      },
    ],
  },
  {
    path: '',
    component: BaseWithMenuLayout,
    canActivate: [AuthGuard],
    children: [],
  },
];
