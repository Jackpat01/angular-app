import {Component, OnInit, inject} from '@angular/core';
import {CommonModule} from '@angular/common';
import {UserService} from '../../services/user';
import {AuthService, UserClaims} from '../../services/auth';

@Component({
  imports: [CommonModule],
  selector: 'ohfi-header-bar',
  styleUrl: './header-bar.css',
  templateUrl: './header-bar.html',
})
export class HeaderBarComponent implements OnInit {
  private userService = inject(UserService);
  private authService = inject(AuthService);
  userClaims: UserClaims | null = null;
  userEmail: string | null = null;
  userRole: string | null = null;
  company: string | null = null;

  ngOnInit(): void {
    // Subscribe to user claims updates
    this.authService.userClaims$.subscribe((claims) => {
      this.userClaims = claims;
      this.userEmail = claims?.email || null;
      this.userRole = claims?.role || null;
      this.company = claims?.company || null;
    });
  }

  /**
   * Example: Get specific claim
   * Replaces old Session["CompanyName"]
   */
  getCompany(): string | null {
    return this.userService.getCompany();
  }

  /**
   * Example: Get user role
   * Replaces old Session["UserRole"]
   */
  getUserRole(): string | null {
    return this.userService.getUserRole();
  }
}
