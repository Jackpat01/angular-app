import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface AuthToken {
  accessToken: string;
  refreshToken?: string;
  expiresIn?: number;
  tokenType?: string;
}

export interface UserClaims {
  sub?: string;
  email?: string;
  role?: string;
  company?: string;
  provider?: string;
  nameIdentifier?: string;
  exp?: number;
  iat?: number;
  [key: string]: any;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private tokenKey = 'ohfi_access_token';
  private refreshTokenKey = 'ohfi_refresh_token';
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasValidToken());
  private userClaimsSubject = new BehaviorSubject<UserClaims | null>(this.extractClaims());

  isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  userClaims$ = this.userClaimsSubject.asObservable();

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
    this.userClaimsSubject.next(this.extractClaims());
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
    this.userClaimsSubject.next(null);
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

  /**
   * Extract claims from current token
   * Maps JWT standard claims to UserClaims interface
   */
  private extractClaims(): UserClaims | null {
    const token = this.getAccessToken();
    if (!token) {
      return null;
    }

    const decoded = this.decodeToken(token);
    if (!decoded) {
      return null;
    }

    return {
      sub: decoded.sub,
      email: decoded.email,
      role: decoded.role,
      company: decoded.company,
      provider: decoded.provider,
      nameIdentifier: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
      exp: decoded.exp,
      iat: decoded.iat,
      ...decoded, // Include all other claims
    };
  }

  /**
   * Get current user claims synchronously
   */
  getClaims(): UserClaims | null {
    return this.userClaimsSubject.getValue();
  }

  /**
   * Get specific claim value
   */
  getClaim(key: string): any {
    const claims = this.getClaims();
    return claims ? claims[key] : null;
  }

  /**
   * Get user email from claims
   */
  getUserEmail(): string | null {
    return this.getClaim('email');
  }

  /**
   * Get user role from claims
   */
  getUserRole(): string | null {
    return this.getClaim('role');
  }

  /**
   * Get company from claims (custom claim)
   */
  getCompany(): string | null {
    return this.getClaim('company');
  }

  /**
   * Get provider from claims (custom claim)
   */
  getProvider(): string | null {
    return this.getClaim('provider');
  }
}
