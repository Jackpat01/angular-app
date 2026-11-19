import {Injectable, inject} from '@angular/core';
import {BehaviorSubject, Observable} from 'rxjs';
import {finalize} from 'rxjs/operators';
import {ApiService} from './api';
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
  private isLoadingSubject = new BehaviorSubject<boolean>(false);
  isLoading$ = this.isLoadingSubject.asObservable();

  loginToPage(credentials: LoginRequest): Observable<LoginResponse> {
    this.isLoadingSubject.next(true);
    return this.apiService
      .post<LoginResponse>('/auth/loginToPage', credentials as unknown as Record<string, unknown>)
      .pipe(finalize(() => this.isLoadingSubject.next(false)));
  }

  getEULAText(): Observable<EULAResponse> {
    return this.apiService.get<EULAResponse>('/EULA/eulaText');
  }

  acceptTermsOfUse(email: string): Observable<AcceptTermsResponse> {
    const request: AcceptTermsRequest = {email};
    return this.apiService.post<AcceptTermsResponse>(
      '/auth/accept-terms',
      request as unknown as Record<string, unknown>,
    );
  }

  isLoading(): boolean {
    return this.isLoadingSubject.value;
  }
}
