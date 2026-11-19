import { Injectable, inject } from '@angular/core';
import { UserService } from './user';

@Injectable({
  providedIn: 'root'
})
export class LogoutService {
  private userService = inject(UserService);

  executeLogout(redirectUrl = '/'): void {
    console.log('Logging out...');
    this.userService.logout().subscribe({
      next: () => {
        this.userService.logoutLocal();
        window.location.href = redirectUrl;
      },
      error: (error) => {
        console.error('Logout failed:', error);
        this.userService.logoutLocal();
        window.location.href = redirectUrl;
      }
    });
  }
}
