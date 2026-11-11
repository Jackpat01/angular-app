import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TitleService } from '../../services/title';
import { UserService } from '../../services/user';
import { AuthService, UserClaims } from '../../services/auth';

@Component({
  imports: [CommonModule],
  selector: 'ohfi-user-dashboard',
  styleUrl: './user-dashboard.css',
  templateUrl: './user-dashboard.html',
})
export class UserDashboard implements OnInit {
  userClaims: UserClaims | null = null;
  dashboardData: any = null;

  constructor(
    private titleService: TitleService,
    private userService: UserService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle('User Dashboard');

    // Example 1: Get claims from AuthService
    // This replaces: Session["UserId"], Session["UserRole"], Session["CompanyName"], etc.
    this.authService.userClaims$.subscribe((claims) => {
      this.userClaims = claims;
      if (claims) {
        this.loadDashboardData(claims);
      }
    });
  }

  /**
   * Example: Load dashboard data using user claims
   * This replaces the old pattern:
   *
   * [Old ASP.NET MVC]
   * public ActionResult Dashboard()
   * {
   *   var userId = (string)Session["UserId"];
   *   var userRole = (string)Session["UserRole"];
   *   var data = GetUserDataByRole(userId, userRole);
   *   return View(data);
   * }
   *
   * [New Angular + JWT]
   * Constructor receives claims via Observable
   * Uses claims to fetch personalized data from API
   */
  private loadDashboardData(claims: UserClaims): void {
    // Example: Use claims to build dashboard
    this.dashboardData = {
      greeting: `Welcome, ${claims.email}!`,
      role: claims.role || 'User',
      company: claims.company || 'N/A',
      claims: claims, // Full claims object for debugging
    };
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
  getAllClaimsKeys(): string[] {
    return this.userClaims ? Object.keys(this.userClaims) : [];
  }
}
