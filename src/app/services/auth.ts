import {Injectable} from '@angular/core';
import {BehaviorSubject} from 'rxjs';

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

  [key: string]: unknown;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private tokenKey = 'ohfi_access_token';
  private refreshTokenKey = 'ohfi_refresh_token';
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasValidToken());
  isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  private userClaimsSubject = new BehaviorSubject<UserClaims | null>(this.extractClaims());
  userClaims$ = this.userClaimsSubject.asObservable();

  setTokens(authToken: AuthToken): void {
    localStorage.setItem(this.tokenKey, authToken.accessToken);
    if (authToken.refreshToken) {
      localStorage.setItem(this.refreshTokenKey, authToken.refreshToken);
    }
    this.isAuthenticatedSubject.next(true);
    this.userClaimsSubject.next(this.extractClaims());
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.refreshTokenKey);
  }

  hasValidToken(): boolean {
    const token = this.getAccessToken();
    if (!token) return false;
    return !this.isTokenExpired(token);
  }

  clearTokens(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.refreshTokenKey);
    this.isAuthenticatedSubject.next(false);
    this.userClaimsSubject.next(null);
  }

  isAuthenticated(): boolean {
    return this.hasValidToken();
  }

  decodeToken(token: string): Record<string, unknown> | null {
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

  isTokenExpired(token?: string): boolean {
    const tokenToCheck = token || this.getAccessToken();
    if (!tokenToCheck) {
      return true;
    }

    const decoded = this.decodeToken(tokenToCheck);
    if (!decoded || !decoded['exp']) {
      return true;
    }

    const currentTime = Math.floor(Date.now() / 1000);
    return (decoded['exp'] as number) < currentTime;
  }

  getClaims(): UserClaims | null {
    return this.userClaimsSubject.getValue();
  }

  getClaim(key: string): unknown {
    const claims = this.getClaims();
    return claims ? claims[key] : null;
  }

  getUserEmail(): string | null {
    const value = this.getClaim('email');
    return typeof value === 'string' ? value : null;
  }

  getUserRole(): string | null {
    const value = this.getClaim('role');
    return typeof value === 'string' ? value : null;
  }

  getCompany(): string | null {
    const value = this.getClaim('company');
    return typeof value === 'string' ? value : null;
  }

  getProvider(): string | null {
    const value = this.getClaim('provider');
    return typeof value === 'string' ? value : null;
  }

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
      sub: decoded['sub'] as string | undefined,
      email: decoded['email'] as string | undefined,
      role: decoded['role'] as string | undefined,
      company: decoded['company'] as string | undefined,
      provider: decoded['provider'] as string | undefined,
      nameIdentifier: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] as string | undefined,
      exp: decoded['exp'] as number | undefined,
      iat: decoded['iat'] as number | undefined,
      ...decoded,
    };
  }
}
