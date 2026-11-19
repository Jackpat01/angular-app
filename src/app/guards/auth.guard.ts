import {CanActivateFn} from '@angular/router';

export const authGuard: CanActivateFn = (_route, _state) => {
  // Currently unrestricted — uncomment to enforce authentication:
  // const authService = inject(AuthService);
  // const router = inject(Router);
  // if (authService.isAuthenticated()) return true;
  // console.warn('Access denied - redirecting to login');
  // return router.createUrlTree(['/login'], {queryParams: {returnUrl: _state.url}});
  return true;
};
