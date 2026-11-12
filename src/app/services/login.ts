import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import { ApiService } from './api';
import { AuthService } from './auth';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface ARUMUser {
  userID: number;
  email: string;
  acceptedTerms: boolean | null;
  token: string;
  ipAddress: string;
}

export interface LoginResponse {
  successful: boolean;
  message: string;
  lstARUMUsers: ARUMUser[];
  results?: {
    successful: boolean;
    message: string;
  };
}

export interface EULAResponse {
  email: string;
  eulaText: string;
}

export interface AcceptTermsRequest {
  email: string;
}

export interface AcceptTermsResponse {
  successful: boolean;
  message: string;
}

@Injectable({
  providedIn: 'root',
})
export class LoginService {
  private isLoadingSubject = new BehaviorSubject<boolean>(false);
  isLoading$ = this.isLoadingSubject.asObservable();

  constructor(
    private apiService: ApiService,
    private authService: AuthService
  ) {}

  /**
   * Login with email and password
   * Endpoint: POST /LoginToPage
   */
  loginToPage(credentials: LoginRequest): Observable<LoginResponse> {
    this.isLoadingSubject.next(true);
    return new Observable(observer => {
      this.apiService.post<LoginResponse>('/LoginToPage', credentials).subscribe({
        next: (response) => {
          this.isLoadingSubject.next(false);
          observer.next(response);
          observer.complete();
        },
        error: (error) => {
          this.isLoadingSubject.next(false);
          observer.error(error);
        }
      });
    });
  }

  /**
   * Get EULA text
   * Endpoint: GET /eula
   */
  getEULAText(email: string): Observable<EULAResponse> {
    return this.apiService.get<EULAResponse>(`/eula?email=${encodeURIComponent(email)}`);
  }

  /**
   * Accept Terms of Use
   * Endpoint: POST /accept-terms
   */
  acceptTermsOfUse(email: string): Observable<AcceptTermsResponse> {
    const request: AcceptTermsRequest = { email };
    return this.apiService.post<AcceptTermsResponse>('/accept-terms', request);
  }

  /**
   * Check if user needs to accept terms
   * Returns true if any ARUM user has not accepted terms
   */
  userNeedsToAcceptTerms(arumUsers: ARUMUser[]): boolean {
    return arumUsers.some(user => user.acceptedTerms === null || user.acceptedTerms === false);
  }

  /**
   * Check if login is currently in progress
   */
  isLoading(): boolean {
    return this.isLoadingSubject.value;
  }
}
