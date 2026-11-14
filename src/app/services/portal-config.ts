import {inject, Injectable} from '@angular/core';
import {BehaviorSubject, firstValueFrom, Observable, of} from 'rxjs';
import {ApiService} from './api';
import {catchError, map} from 'rxjs/operators';
import {Classification} from '../models/Classification';
import {PortalConfig} from '../models/portal-config';

@Injectable({
  providedIn: 'root',
})
export class PortalConfigService {
  private defaultConfig: PortalConfig = {
    WebPortalTitle: 'OMES Cost Report and Rate Setting',
    applicationName: 'OMES Cost Report and Rate Setting',
    loginMessageHyperLink: 'https://myersandstauffer.com/wp-content/uploads/2021/09/ExternalLoginMessage.txt',
    loginSessionKey: 'ClassificationSession',
    classificationReplacementTerm: '',
    allClassifications: [],
    enableBB: false,
    enableResource: false,
    auditTypeReplacementName: '',
  };

  private apiService=inject(ApiService);
  private configSubject = new BehaviorSubject<PortalConfig>(this.defaultConfig);
  config$ = this.configSubject.asObservable();

  constructor() {
    this.loadConfig();
  }

  /**
   * Get the current portal configuration (never null)
   */
  getConfig(): PortalConfig {
    return this.configSubject.value;
  }

  getAuditTypeReplacementName(): string {
    return this.configSubject.value.auditTypeReplacementName;
  }

  enableBB():boolean {
    return this.configSubject.value.enableBB;
  }
  enableResource():boolean {
    return this.configSubject.value.enableResource;
  }
  /**
   * Get the application name from config
   */
  getApplicationName(): string {
    return this.configSubject.value.applicationName;
  }

  getWebPortalTitle(): string {
    return this.configSubject.value.WebPortalTitle;
  }

  getClassificationReplacementTerm(): string {
    return this.configSubject.value.classificationReplacementTerm;

  }

  getAllClassifications(): Classification[] {
    return this.configSubject.value.allClassifications;
  }

  async retrieveLoginMessage(): Promise<string[]> {
    const messageHyperlink = this.configSubject.value.loginMessageHyperLink;

    if (!messageHyperlink) {
      return [];
    }

    return firstValueFrom(
      this.apiService.getRemoteData(messageHyperlink).pipe(
        map((responseText: string) => {
          // Split on \r\n, \r, or \n and filter out empty entries
          return responseText
            .split(/\r?\n|\r/)
            .filter(line => line.trim().length > 0);
        }),
        catchError(() => {
          // Equivalent to the empty catch block returning an empty array
          return of([]);
        })
      )
    );
  }

  getClassificationSessionKey(): string {
    return this.configSubject.value.loginSessionKey;
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
}
