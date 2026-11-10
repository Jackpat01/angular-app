import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap, map } from 'rxjs';
import { ApiService } from './api';
import { AuthService, AuthToken, UserClaims } from './auth';

export interface UserInfo {
  email: string;
  role: string;
  providerType: string;
  projectName: string;
  isAuthenticated: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  user: UserInfo;
  token: AuthToken;
}

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private userSubject = new BehaviorSubject<UserInfo>({
    email: 'user@example.com',
    role: 'User',
    providerType: 'Standard',
    projectName: 'Main Project',
    isAuthenticated: false,
  });

  user$ = this.userSubject.asObservable();

  constructor(
    private apiService: ApiService,
    private authService: AuthService
  ) {
    this.initializeUserFromClaims();
  }

  /**
   * Initialize user data from JWT claims
   * This replaces session variables from old ASP.NET pattern
   */
  private initializeUserFromClaims(): void {
    if (!this.authService.isAuthenticated()) {
      return;
    }

    const claims = this.authService.getClaims();
    if (claims) {
      this.userSubject.next({
        email: claims.email || '',
        role: claims.role || 'User',
        providerType: claims.provider || 'Standard',
        projectName: claims.company || 'Main Project',
        isAuthenticated: true,
      });
    }
  }

  /**
   * Load current user from API
   * Endpoint: GET /api/auth/user
   */
  private loadCurrentUser(): void {
    if (!this.authService.isAuthenticated()) {
      return;
    }

    this.apiService.get<UserInfo>('/auth/user').subscribe({
      next: (user) => {
        this.userSubject.next({ ...user, isAuthenticated: true });
      },
      error: (error) => {
        console.error('Failed to load current user', error);
        // User stays logged out on error
        this.authService.clearTokens();
      }
    });
  }

  /**
   * Login user
   * Endpoint: POST /api/auth/login
   * Stores JWT token which contains claims (replaces session variables)
   */
  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.apiService.post<LoginResponse>('/auth/login', credentials).pipe(
      tap((response) => {
        // Store tokens in localStorage
        this.authService.setTokens(response.token);
        // Set user data
        this.userSubject.next({ ...response.user, isAuthenticated: true });
      })
    );
  }

  /**
   * Logout user
   * Endpoint: POST /api/auth/logout
   */
  logout(): Observable<any> {
    return this.apiService.post('/auth/logout', {}).pipe(
      tap(() => {
        this.logoutLocal();
      })
    );
  }

  getUser(): UserInfo {
    return this.userSubject.value;
  }

  setUser(user: UserInfo): void {
    this.userSubject.next(user);
  }

  setAuthenticated(isAuth: boolean): void {
    const current = this.userSubject.value;
    this.userSubject.next({ ...current, isAuthenticated: isAuth });
  }

  /**
   * Get user claims from JWT token
   * Replaces Session["UserId"], Session["UserRole"], etc.
   */
  getUserClaims(): UserClaims | null {
    return this.authService.getClaims();
  }

  /**
   * Get specific claim value by key
   * Example: getClaim('company') replaces Session["CompanyName"]
   */
  getClaim(key: string): any {
    return this.authService.getClaim(key);
  }

  /**
   * Get user email from JWT claims
   */
  getUserEmail(): string | null {
    return this.authService.getUserEmail();
  }

  /**
   * Get user role from JWT claims
   */
  getUserRole(): string | null {
    return this.authService.getUserRole();
  }

  /**
   * Get company/organization from JWT claims
   */
  getCompany(): string | null {
    return this.authService.getCompany();
  }

  /**
   * Get provider type from JWT claims
   */
  getProvider(): string | null {
    return this.authService.getProvider();
  }

  logoutLocal(): void {
    this.authService.clearTokens();
    this.userSubject.next({
      email: '',
      role: '',
      providerType: '',
      projectName: '',
      isAuthenticated: false,
    });
  }
}
