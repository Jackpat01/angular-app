import {inject, Injectable} from '@angular/core';
import {Observable} from 'rxjs';
import {ApiService} from './api';
import {Classification} from '../models/Classification';
import {PortalConfig} from '../models/portal-config';
import {BaseConfigService} from './base-config.service';

@Injectable({
  providedIn: 'root',
})
export class PortalConfigService extends BaseConfigService<PortalConfig> {
  private apiService = inject(ApiService);

  constructor() {
    const defaultConfig: PortalConfig = {
      webPortalTitle: 'test',
      application: 'OMES Cost Report and Rate Setting',
      loginMessage: ['test1', 'test2', 'test3'],
      classificationSessionKey: 'MSLCStatlogClassification',
      classificationReplacementTerm: 'Project Type',
      allClassifications: [],
      enableBB: true,
      enableResource: true,
      auditTypeReplacementName: 'StatLog Type',
    };

    super({
      defaultValue: defaultConfig,
      endpoint: '/portalConfiguration/get-config'
    });
    this.loadConfig();
  }

  protected loadFromApi(): Observable<PortalConfig> {
    const defaultConfig = this.defaultValue;
    return new Observable(observer => {
      this.apiService.get<PortalConfig>(this.endpoint).subscribe({
        next: (config) => {
          const mappedConfig: PortalConfig = {
            webPortalTitle: (config as any).WebPortalTitle || config.webPortalTitle || defaultConfig.webPortalTitle,
            application: (config as any).ApplicationName || config.application || defaultConfig.application,
            loginMessage: (config as any).LoginMessageHyperLink || (config as any).loginMessage || (config as any).loginmessage || config.loginMessage || defaultConfig.loginMessage,
            classificationSessionKey: (config as any).ClassificationSessionKey || config.classificationSessionKey || defaultConfig.classificationSessionKey,
            classificationReplacementTerm: (config as any).ClassificationReplacementTerm || config.classificationReplacementTerm || defaultConfig.classificationReplacementTerm,
            allClassifications: (config as any).AllClassifications || config.allClassifications || defaultConfig.allClassifications,
            enableBB: (config as any).EnableBB !== undefined ? (config as any).EnableBB : config.enableBB,
            enableResource: (config as any).EnableResource !== undefined ? (config as any).EnableResource : config.enableResource,
            auditTypeReplacementName: (config as any).AuditTypeReplacementName || config.auditTypeReplacementName || defaultConfig.auditTypeReplacementName,
          };
          observer.next(mappedConfig);
          observer.complete();
        },
        error: (error) => {
          observer.error(error);
        }
      });
    });
  }

  getAuditTypeReplacementName(): string {
    return this.getConfig().auditTypeReplacementName;
  }

  enableBB(): boolean {
    return this.getConfig().enableBB;
  }

  enableResource(): boolean {
    return this.getConfig().enableResource;
  }

  getApplicationName(): string {
    return this.getConfig().application;
  }

  getWebPortalTitle(): string {
    return this.getConfig().webPortalTitle;
  }

  getClassificationReplacementTerm(): string {
    return this.getConfig().classificationReplacementTerm;
  }

  getAllClassifications(): Classification[] {
    return this.getConfig().allClassifications;
  }

  retrieveLoginMessage(): string[] {
    return this.getConfig().loginMessage;
  }

  getClassificationSessionKey(): string {
    return this.getConfig().classificationSessionKey;
  }
}

