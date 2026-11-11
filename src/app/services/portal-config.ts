import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ApiService } from './api';

export interface PortalConfig {
  applicationName: string;
  [key: string]: any;
}

@Injectable({
  providedIn: 'root',
})
export class PortalConfigService {
  private configSubject = new BehaviorSubject<PortalConfig | null>(null);
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
        this.configSubject.next(null);
      }
    });
  }

  /**
   * Get the current portal configuration
   */
  getConfig(): PortalConfig | null {
    return this.configSubject.value;
  }

  /**
   * Get the application name from config
   */
  getApplicationName(): string {
    return this.configSubject.value?.applicationName || 'OHFI Application';
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
