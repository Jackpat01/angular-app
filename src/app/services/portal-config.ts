import {inject, Injectable} from '@angular/core';
import {BehaviorSubject, firstValueFrom, Observable, of} from 'rxjs';
import {ApiService} from './api';
import {catchError, filter, map, skip} from 'rxjs/operators';
import {Classification} from '../models/Classification';
import {PortalConfig} from '../models/portal-config';

@Injectable({
  providedIn: 'root',
})
export class PortalConfigService {
  private defaultConfig: PortalConfig = {
    webPortalTitle: 'test',
    application: 'OMES Cost Report and Rate Setting',
    loginMessage: '',
    classificationSessionKey: 'MSLCStatlogClassification',
    classificationReplacementTerm: 'Project Type',
    allClassifications: [],
    enableBB: true,
    enableResource: true,
    auditTypeReplacementName: 'StatLog Type',
  };

  private apiService = inject(ApiService);
  private configSubject = new BehaviorSubject<PortalConfig>(this.defaultConfig);
  config$ = this.configSubject.asObservable();
  private configLoadedSubject = new BehaviorSubject<boolean>(false);
  configLoaded$ = this.configLoadedSubject.asObservable();

  constructor() {
    this.loadConfig();
  }

  /**
   * Check if config has been loaded from API
   */
  isConfigLoaded(): boolean {
    return this.configLoadedSubject.value;
  }

  /**
   * Wait for config to load from API
   */
  async waitForConfigLoad(): Promise<void> {
    if (this.isConfigLoaded()) {
      return;
    }
    return firstValueFrom(this.configLoaded$.pipe(
      skip(1),
      filter(loaded => loaded === true)
    )).then(() => {});
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

  enableBB(): boolean {
    return this.configSubject.value.enableBB;
  }

  enableResource(): boolean {
    return this.configSubject.value.enableResource;
  }

  /**
   * Get the application name from config
   */
  getApplicationName(): string {
    return this.configSubject.value.application;
  }

  getWebPortalTitle(): string {
    return this.configSubject.value.webPortalTitle;
  }

  getClassificationReplacementTerm(): string {
    return this.configSubject.value.classificationReplacementTerm;

  }

  getAllClassifications(): Classification[] {
    return this.configSubject.value.allClassifications;
  }

  async retrieveLoginMessage(): Promise<string[]> {
    try {
      const result = this.configSubject.value.loginMessage
        .split(/\r?\n|\r/)
        .filter(line => line.trim().length > 0);

      return Promise.resolve(result);
    } catch {
      return Promise.resolve([]);
    }
  }

  getClassificationSessionKey(): string {
    return this.configSubject.value.classificationSessionKey;
  }


  /**
   * Refresh configuration from API
   */
  refreshConfig(): Observable<PortalConfig> {
    return new Observable(observer => {
      this.apiService.get<PortalConfig>('/portalConfiguration/get-config').subscribe({
        next: (config) => {
          this.configSubject.next(config);
          this.configLoadedSubject.next(true);
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
    this.apiService.get<PortalConfig>('/portalConfiguration/get-config').subscribe({
      next: (config) => {
        console.log('Portal config received from API:', config);
        console.log('WebPortalTitle from API:', (config as any).WebPortalTitle || (config as any).webPortalTitle);
        
        // Map API response to our interface - API might use different casing
        const mappedConfig: PortalConfig = {
          webPortalTitle: (config as any).WebPortalTitle || config.webPortalTitle || this.defaultConfig.webPortalTitle,
          application: (config as any).ApplicationName || config.application || this.defaultConfig.application,
          loginMessage: (config as any).LoginMessageHyperLink || (config as any).loginmessage || config.loginMessage || this.defaultConfig.loginMessage,
          classificationSessionKey: (config as any).ClassificationSessionKey || config.classificationSessionKey || this.defaultConfig.classificationSessionKey,
          classificationReplacementTerm: (config as any).ClassificationReplacementTerm || config.classificationReplacementTerm || this.defaultConfig.classificationReplacementTerm,
          allClassifications: (config as any).AllClassifications || config.allClassifications || this.defaultConfig.allClassifications,
          enableBB: (config as any).EnableBB !== undefined ? (config as any).EnableBB : config.enableBB,
          enableResource: (config as any).EnableResource !== undefined ? (config as any).EnableResource : config.enableResource,
          auditTypeReplacementName: (config as any).AuditTypeReplacementName || config.auditTypeReplacementName || this.defaultConfig.auditTypeReplacementName,
        };
        
        console.log('Mapped config:', mappedConfig);
        this.configSubject.next(mappedConfig);
        this.configLoadedSubject.next(true);
      },
      error: (error) => {
        console.error('Failed to load portal config from API', error);
        console.warn('Using default portal configuration');
        // Keep using default config on error, but mark as "loaded" to prevent blocking
        this.configLoadedSubject.next(true);
      }
    });
  }
}
