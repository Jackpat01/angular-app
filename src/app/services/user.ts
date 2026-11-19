import {Injectable, inject} from '@angular/core';
import {BehaviorSubject, Observable, tap} from 'rxjs';
import {ApiService} from './api';
import {AuthService, AuthToken, UserClaims} from './auth';

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
  private apiService = inject(ApiService);
  private authService = inject(AuthService);
  private userSubject = new BehaviorSubject<UserInfo>({
    email: 'user@example.com',
    role: 'User',
    providerType: 'Standard',
    projectName: 'Main Project',
    isAuthenticated: false,
  });

  user$ = this.userSubject.asObservable();

  constructor() {
    this.initializeUserFromClaims();
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.apiService
      .post<LoginResponse>('/auth/login', credentials as unknown as Record<string, unknown>)
      .pipe(
        tap((response: LoginResponse) => {
          this.authService.setTokens(response.token);
          this.userSubject.next({...response.user, isAuthenticated: true});
        }),
      );
  }

  logout(): Observable<Record<string, unknown>> {
    return this.apiService.post<Record<string, unknown>>('/auth/logout', {}).pipe(
      tap(() => {
        this.logoutLocal();
      }),
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
    this.userSubject.next({...current, isAuthenticated: isAuth});
  }

  getUserClaims(): UserClaims | null {
    return this.authService.getClaims();
  }

  getClaim(key: string): unknown {
    return this.authService.getClaim(key);
  }

  getUserEmail(): string | null {
    return this.authService.getUserEmail();
  }

  getUserRole(): string | null {
    return this.authService.getUserRole();
  }

  getCompany(): string | null {
    return this.authService.getCompany();
  }

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
}
