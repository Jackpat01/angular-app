import {Routes} from '@angular/router';
import {authGuard} from './guards/auth.guard';
import {projectSelectionGuard} from './guards/project-selection.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layouts/base-layout/base-layout').then(m => m.BaseLayout),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/home/home').then(m => m.Home),
        children: [
          {path: '', redirectTo: 'login', pathMatch: 'full'},
          {
            path: 'login',
            loadComponent: () =>
              import('./pages/home/login/login').then(m => m.Login),
          },
          {
            path: 'accept',
            loadComponent: () =>
              import('./pages/home/accept-terms/accept-terms').then(m => m.AcceptTerms),
          },
          {
            path: 'error',
            loadComponent: () =>
              import('./pages/home/error/error').then(m => m.ErrorPage),
          },
        ],
      },
    ],
  },
  {
    path: '',
    loadComponent: () =>
      import('./layouts/base-with-project-layout/base-with-project-layout').then(
        m => m.BaseWithProjectLayout,
      ),
    canActivate: [authGuard],
    children: [
      {
        path: 'projects',
        loadComponent: () =>
          import('./pages/projects/projects').then(m => m.Projects),
        data: {bodyClass: 'layout-projects'},
      },
      {
        path: 'projects/set-project',
        loadComponent: () =>
          import('./pages/projects/set-project/set-project').then(m => m.SetProjectComponent),
        canActivate: [projectSelectionGuard],
      },
    ],
  },
  {
    path: '',
    loadComponent: () =>
      import('./layouts/base-with-menu-layout/base-with-menu-layout').then(
        m => m.BaseWithMenuLayout,
      ),
    canActivate: [authGuard],
    children: [
      {
        path: 'bulletinBoard/showAll',
        loadComponent: () =>
          import('./pages/bulletin-board/show-all/show-all').then(m => m.ShowAll),
      },
    ],
  },
];
