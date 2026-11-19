import {Routes} from '@angular/router';
import {BaseLayout} from './layouts/base-layout/base-layout';
import {BaseWithProjectLayout} from './layouts/base-with-project-layout/base-with-project-layout';
import {BaseWithMenuLayout} from './layouts/base-with-menu-layout/base-with-menu-layout';
import {Home} from './features/home/pages/home/home';
import {Login} from './features/home/pages/login/login';
import {ErrorPage} from './features/home/pages/error/error';
import {AuthGuard} from './core/guards/auth.guard';
import {AcceptTerms} from './features/home/pages/accept-terms/accept-terms';
import {Projects} from './features/projects/pages/projects/projects';
import {SetProjectComponent} from './features/projects/pages/set-project/set-project';
import {ProjectSelectionGuard} from './features/projects/guards/project-selection.guard';
import {ShowAll} from './features/bulletin-board/pages/show-all/show-all';
import {Secure} from './features/secure/secure';
import {StatLog} from './features/stat-log/stat-log';

export const routes: Routes = [
  {
    path: '',
    component: BaseLayout,
    children: [
      {
        path: '',
        component: Home,
        children: [
          {path: '', redirectTo: 'login', pathMatch: 'full'},
          {path: 'login', component: Login},
          {path: 'accept', component: AcceptTerms},
          {path: 'error', component: ErrorPage},
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
        data: {bodyClass: 'layout-projects'},
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
    children: [
      {
        path: 'bulletinBoard/showAll',
        component: ShowAll,
      },
      {
        path: 'secure',
        component: Secure,
      },
      {
        path: 'stat-log',
        component: StatLog,
      }
    ],
  }];
