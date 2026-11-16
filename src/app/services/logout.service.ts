import { Injectable, inject } from '@angular/core';
import { UserService } from './user';

/**
 * Service that handles logout operations with common patterns.
 * Consolidates duplicate logout logic across components.
 */
@Injectable({
  providedIn: 'root'
})
export class LogoutService {
  private userService = inject(UserService);

  /**
   * Execute logout with API call and cleanup
   * Handles both success and error cases by clearing local session data
   * and redirecting to specified URL
   */
  executeLogout(redirectUrl: string = '/'): void {
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
