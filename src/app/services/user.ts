import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ApiService } from './api';

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
  token?: string;
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

  constructor(private apiService: ApiService) {
    this.loadCurrentUser();
  }

  /**
   * Load current user from API
   * Endpoint: GET /api/auth/user
   */
  private loadCurrentUser(): void {
    this.apiService.get<UserInfo>('/auth/user').subscribe({
      next: (user) => {
        this.userSubject.next(user);
      },
      error: (error) => {
        console.error('Failed to load current user', error);
        // User stays logged out on error
      }
    });
  }

  /**
   * Login user
   * Endpoint: POST /api/auth/login
   */
  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.apiService.post<LoginResponse>('/auth/login', credentials);
  }

  /**
   * Logout user
   * Endpoint: POST /api/auth/logout
   */
  logout(): Observable<any> {
    return this.apiService.post('/auth/logout', {});
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

  logoutLocal(): void {
    this.userSubject.next({
      email: '',
      role: '',
      providerType: '',
      projectName: '',
      isAuthenticated: false,
    });
  }
}
