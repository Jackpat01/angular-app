import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ApiService } from './api';

export interface AppConfig {
  appVersion: string;
  currentYear: number;
  appName: string;
  supportEmail: string;
  WebPortalTitle:string;
}

@Injectable({
  providedIn: 'root',
})
export class AppConfigService {
  private configSubject = new BehaviorSubject<AppConfig>({
    appVersion: '1.0.0',
    currentYear: new Date().getFullYear(),
    appName: 'Web Portal',
    supportEmail: 'support@example.com',WebPortalTitle:'test'
  });

  config$ = this.configSubject.asObservable();

  constructor(private apiService: ApiService) {
    this.loadConfig();
  }

  /**
   * Load configuration from API
   * Endpoint: GET /api/config
   */
  private loadConfig(): void {
    this.apiService.get<AppConfig>('/config').subscribe({
      next: (config) => {
        this.configSubject.next(config);
      },
      error: (error) => {
        console.error('Failed to load config from API, using defaults', error);
        // Keep using default values on error
      }
    });
  }

  getConfig(): AppConfig {
    return this.configSubject.value;
  }

  setAppVersion(version: string): void {
    const current = this.configSubject.value;
    this.configSubject.next({ ...current, appVersion: version });
  }

  setSupportEmail(email: string): void {
    const current = this.configSubject.value;
    this.configSubject.next({ ...current, supportEmail: email });
  }
}
