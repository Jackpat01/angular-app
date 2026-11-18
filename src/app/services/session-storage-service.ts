import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ApiService } from './api';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ARUMProxy_User } from '../models/ARUMProxy_User';
import { Classification } from '../models/Classification';
import { ARUMRole } from '../models/ARUMRole';
import { PortalConfigService } from './portal-config';

@Injectable({
  providedIn: 'root',
})
export class SessionStorageService {
  private platformId = inject(PLATFORM_ID);
  private apiService = inject(ApiService);
  private portalConfig = inject(PortalConfigService);

  canAccessFeature(featureName: string): Observable<boolean> {
    const userRoleNames: string[] = this.userRoleNames;
    const payload = {
      featureName: featureName,
      userRoleNames: userRoleNames,
      classification: this.classification,
    };

    return this.apiService.post<{ canAccess: boolean }>('/retrieveSession/canAccess', payload).pipe(
      map((response) => response.canAccess),
      catchError((error) => {
        console.error(`Error checking access for ${featureName}:`, error);
        return of(false);
      }),
    );
  }

  get userRoleNames(): string[] {
    return (this.userRoles as ARUMRole[]).map((role) => role.roleName);
  }

  get userRoles(): ARUMRole[] {
    const arumUser: ARUMProxy_User | null = this.getItem<ARUMProxy_User>('arumUser');
    let arumRoleArray: ARUMRole[] | null;
    if (arumUser == null) {
      arumRoleArray = null;
    } else {
      const roles: ARUMRole[] = arumUser.roles;
      arumRoleArray = roles ? [...roles].sort((a, b) => b.order - a.order) : null;
    }
    return arumRoleArray ?? [];
  }

  getSimplifiedType(value: Classification | null): string | null {
    if (value) {
      switch (value.type) {
        case 'ICF_ProdRestore':
          return 'ICF';
        case 'NF_ProdRestore':
          return 'NF';
        default:
          return value.type;
      }
    }
    return null;
  }
  getUserHasRole(roleName: string | null): boolean {
    const userRoles: ARUMRole[] = this.userRoles;
    return (
      userRoles?.some((role) => role.roleName?.trim().toUpperCase() === roleName?.toUpperCase()) ??
      false
    );
  }

  get isState(): boolean {
    return this.getUserHasRole('STATE');
  }

  get isNF(): boolean {
    return this.getSimplifiedType(this.classification)?.toLowerCase() === 'nf';
  }

  get isICF(): boolean {
    return this.getSimplifiedType(this.classification)?.toLowerCase() === 'icf';
  }

  get classification(): Classification | null {
    const classification = this.getItem<Classification>(
      this.portalConfig.getClassificationSessionKey(),
    );
    if (classification !== null) return classification;
    const allClassifications = this.portalConfig.getAllClassifications();
    if (allClassifications.length !== 1) return null;
    return allClassifications[0];
  }

  checkUserRoles(_role: string): Observable<boolean> {
    return this.apiService
      .post<{ success: boolean }>('/ohfiSession/checkUserRoles', {
        _role,
      })
      .pipe(
        map((response) => response.success),
        catchError(() => of(false)),
      );
  }

  getItem<T>(key: string): T | null {
    // Ensure code only runs in the browser
    if (!isPlatformBrowser(this.platformId)) {
      return null;
    }

    const item = sessionStorage.getItem(key);
    if (!item) return null;

    try {
      return JSON.parse(item) as T;
    } catch {
      // If it's a plain string that wasn't JSON stringified, return it as-is
      return item as unknown as T;
    }
  }

  setItem(key: string, value: unknown): void {
    if (isPlatformBrowser(this.platformId)) {
      const serialized = typeof value === 'string' ? value : JSON.stringify(value);
      sessionStorage.setItem(key, serialized);
    }
  }

  removeItem(key: string): void {
    if (isPlatformBrowser(this.platformId)) {
      sessionStorage.removeItem(key);
    }
  }
}
