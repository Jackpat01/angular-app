import {Component, OnInit, inject, ChangeDetectionStrategy} from '@angular/core';
import {CommonModule} from '@angular/common';
import {TitleService} from '../../services/title';
import {UserService} from '../../services/user';
import {AuthService, UserClaims} from '../../services/auth';

@Component({
  imports: [CommonModule],
  selector: 'ohfi-user-dashboard',
  styleUrl: './user-dashboard.css',
  templateUrl: './user-dashboard.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserDashboard implements OnInit {
  private titleService = inject(TitleService);
  private userService = inject(UserService);
  private authService = inject(AuthService);
  
  userClaims$ = this.authService.userClaims$;

  ngOnInit(): void {
    this.titleService.setTitle('User Dashboard');
  }

  /**
   * Example: Get specific claim
   * Replaces: (string)Session["UserRole"]
   */
  getUserRole(): string | null {
    return this.userService.getUserRole();
  }

  /**
   * Example: Get another claim
   * Replaces: (string)Session["CompanyName"]
   */
  getCompany(): string | null {
    return this.userService.getCompany();
  }

  /**
   * Example: Access all claims
   * Replaces: Session.Keys enumeration
   */
  getAllClaimsKeys(claims: UserClaims | null): string[] {
    return claims ? Object.keys(claims) : [];
  }
}
