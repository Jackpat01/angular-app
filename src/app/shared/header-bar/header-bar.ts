import {Component, OnInit, inject, ChangeDetectionStrategy} from '@angular/core';
import {CommonModule} from '@angular/common';
import {UserService} from '../../services/user';
import {AuthService, UserClaims} from '../../services/auth';
import {map} from 'rxjs/operators';

@Component({
  imports: [CommonModule],
  selector: 'ohfi-header-bar',
  styleUrl: './header-bar.css',
  templateUrl: './header-bar.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeaderBarComponent implements OnInit {
  private userService = inject(UserService);
  private authService = inject(AuthService);
  
  userClaims$ = this.authService.userClaims$;
  userEmail$ = this.authService.userClaims$.pipe(map(claims => claims?.email || null));
  userRole$ = this.authService.userClaims$.pipe(map(claims => claims?.role || null));
  company$ = this.authService.userClaims$.pipe(map(claims => claims?.company || null));

  ngOnInit(): void {
    // No longer needed - async pipe handles subscriptions
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
