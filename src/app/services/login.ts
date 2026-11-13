import {Injectable, inject} from '@angular/core';
import {BehaviorSubject, Observable} from 'rxjs';
import {ApiService} from './api';
import {AuthService} from './auth';
import {AcceptTermsResponse} from '../models/acceptTermsResponse';
import {AcceptTermsRequest} from '../models/acceptTermsRequest';
import {EULAResponse} from '../models/EULAResponse';
import {LoginResponse} from '../models/loginResponse';
import {LoginRequest} from '../models/loginRequest';

@Injectable({
  providedIn: 'root',
})
export class LoginService {
  private apiService = inject(ApiService);
  private authService = inject(AuthService);
  private isLoadingSubject = new BehaviorSubject<boolean>(false);
  isLoading$ = this.isLoadingSubject.asObservable();

  /**
   * Login with email and password
   * Endpoint: POST /LoginToPage
   */
  loginToPage(credentials: LoginRequest): Observable<LoginResponse> {
    this.isLoadingSubject.next(true);
    return new Observable(observer => {
      this.apiService.post<LoginResponse>('/loginToPage', credentials).subscribe({
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
  getEULAText(): Observable<EULAResponse> {
    return this.apiService.get<EULAResponse>('/eulaText');
  }

  /**
   * Accept Terms of Use
   * Endpoint: POST /accept-terms
   */
  acceptTermsOfUse(email: string): Observable<AcceptTermsResponse> {
    const request: AcceptTermsRequest = {email};
    return this.apiService.post<AcceptTermsResponse>('/accept-terms', request);
  }


  /**
   * Check if login is currently in progress
   */
  isLoading(): boolean {
    return this.isLoadingSubject.value;
  }
}
