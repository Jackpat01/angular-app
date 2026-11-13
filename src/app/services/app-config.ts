import {Injectable, inject} from '@angular/core';
import {BehaviorSubject} from 'rxjs';
import {ApiService} from './api';

export interface AppConfig {
  appVersion: string;
  currentYear: number;
  appName: string;
  supportEmail: string;
}

@Injectable({
  providedIn: 'root',
})
export class AppConfigService {
  private apiService = inject(ApiService);
  private configSubject = new BehaviorSubject<AppConfig>({
    appVersion: '0.0.1',
    currentYear: new Date().getFullYear(),
    appName: 'Web Portal',
    supportEmail: 'support@example.com',
  });

  config$ = this.configSubject.asObservable();

  constructor() {
    this.loadConfig();
  }

  getConfig(): AppConfig {
    return this.configSubject.value;
  }

  getAppVersion(): string {
    return this.configSubject.value.appVersion;
  }

  getAppName(): string {
    return this.configSubject.value.appName;
  }

  getCurrentYear(): number {
    return this.configSubject.value.currentYear;
  }

  getSupportEmail(): string {
    return this.configSubject.value.supportEmail;
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
}
