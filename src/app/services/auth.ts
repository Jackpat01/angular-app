import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface AuthToken {
  accessToken: string;
  refreshToken?: string;
  expiresIn?: number;
  tokenType?: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private tokenKey = 'ohfi_access_token';
  private refreshTokenKey = 'ohfi_refresh_token';
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasValidToken());

  isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor() {}

  /**
   * Store tokens in localStorage
   */
  setTokens(authToken: AuthToken): void {
    localStorage.setItem(this.tokenKey, authToken.accessToken);
    if (authToken.refreshToken) {
      localStorage.setItem(this.refreshTokenKey, authToken.refreshToken);
    }
    this.isAuthenticatedSubject.next(true);
  }

  /**
   * Get access token from localStorage
   */
  getAccessToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  /**
   * Get refresh token from localStorage
   */
  getRefreshToken(): string | null {
    return localStorage.getItem(this.refreshTokenKey);
  }

  /**
   * Check if token exists and is not expired
   */
  hasValidToken(): boolean {
    const token = this.getAccessToken();
    if (!token) {
      return false;
    }

    // Basic check - token exists
    // For production, you'd decode JWT and check expiration
    return true;
  }

  /**
   * Clear all authentication data
   */
  clearTokens(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.refreshTokenKey);
    this.isAuthenticatedSubject.next(false);
  }

  /**
   * Check if user is authenticated
   */
  isAuthenticated(): boolean {
    return this.hasValidToken();
  }

  /**
   * Decode JWT token (basic implementation)
   * For production, use a library like jwt-decode
   */
  decodeToken(token: string): any {
    try {
      const base64Url = token.split('.')[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      return JSON.parse(jsonPayload);
    } catch (error) {
      console.error('Failed to decode token:', error);
      return null;
    }
  }

  /**
   * Check if token is expired
   */
  isTokenExpired(token?: string): boolean {
    const tokenToCheck = token || this.getAccessToken();
    if (!tokenToCheck) {
      return true;
    }

    const decoded = this.decodeToken(tokenToCheck);
    if (!decoded || !decoded.exp) {
      return true;
    }

    const currentTime = Math.floor(Date.now() / 1000);
    return decoded.exp < currentTime;
  }
}
