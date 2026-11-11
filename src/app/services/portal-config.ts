import {Injectable} from '@angular/core';
import {BehaviorSubject, Observable} from 'rxjs';
import {ApiService} from './api';

export interface PortalConfig {
  applicationName: string;
  loginMessage: string[];

  [key: string]: any;
}

@Injectable({
  providedIn: 'root',
})
export class PortalConfigService {
  private defaultConfig: PortalConfig = {
    applicationName: 'OHFI Application',
    loginMessage: [
      'Legal Notice',
      'This system is for authorized users only, and its use may be monitored. Unauthorized or improper use may result in disciplinary action, civil/criminal penalties, ',
      'and sanctions. By using this system, you consent to the terms and conditions of use.'
    ],
  };

  private configSubject = new BehaviorSubject<PortalConfig>(this.defaultConfig);
  config$ = this.configSubject.asObservable();

  constructor(private apiService: ApiService) {
    this.loadConfig();
  }

  /**
   * Load portal configuration from API
   * Endpoint: GET /app/portal-config
   */
  private loadConfig(): void {
    this.apiService.get<PortalConfig>('/app/portal-config').subscribe({
      next: (config) => {
        this.configSubject.next(config);
      },
      error: (error) => {
        console.error('Failed to load portal config from API', error);
        // Keep using default config on error
      }
    });
  }

  /**
   * Get the current portal configuration (never null)
   */
  getConfig(): PortalConfig {
    return this.configSubject.value;
  }

  /**
   * Get the application name from config
   */
  getApplicationName(): string {
    return this.configSubject.value.applicationName;
  }

  /**
   * Get the login message from config
   */
  getLoginMessage(): string[] {
    return this.configSubject.value.loginMessage;
  }

  /**
   * Refresh configuration from API
   */
  refreshConfig(): Observable<PortalConfig> {
    return new Observable(observer => {
      this.apiService.get<PortalConfig>('/app/portal-config').subscribe({
        next: (config) => {
          this.configSubject.next(config);
          observer.next(config);
          observer.complete();
        },
        error: (error) => {
          console.error('Failed to refresh portal config', error);
          observer.error(error);
        }
      });
    });
  }
}
