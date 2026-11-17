import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api';
import { Classification } from '../models/Classification';
import { PortalConfig } from '../models/portal-config';
import { BaseConfigService } from './base-config.service';

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
      endpoint: '/portalConfiguration/get-config',
    });
    this.loadConfig();
  }

  protected loadFromApi(): Observable<PortalConfig> {
    const defaultConfig = this.defaultValue;
    return new Observable((observer) => {
      this.apiService.get<PortalConfig>(this.endpoint).subscribe({
        next: (config) => {
          const loginMessageValue =
            ((config as unknown as Record<string, unknown>)['loginMessage'] as string[]) ||
            ((config as unknown as Record<string, unknown>)['loginmessage'] as string[]) ||
            config.loginMessage ||
            defaultConfig.loginMessage;

          console.log('API Response:', config);
          console.log('Mapped loginMessage:', loginMessageValue);

          const mappedConfig: PortalConfig = {
            webPortalTitle:
              ((config as unknown as Record<string, unknown>)['WebPortalTitle'] as string) ||
              config.webPortalTitle ||
              defaultConfig.webPortalTitle,
            application:
              ((config as unknown as Record<string, unknown>)['ApplicationName'] as string) ||
              config.application ||
              defaultConfig.application,
            loginMessage: loginMessageValue,
            classificationSessionKey:
              ((config as unknown as Record<string, unknown>)[
                'ClassificationSessionKey'
              ] as string) ||
              config.classificationSessionKey ||
              defaultConfig.classificationSessionKey,
            classificationReplacementTerm:
              ((config as unknown as Record<string, unknown>)[
                'ClassificationReplacementTerm'
              ] as string) ||
              config.classificationReplacementTerm ||
              defaultConfig.classificationReplacementTerm,
            allClassifications:
              ((config as unknown as Record<string, unknown>)[
                'AllClassifications'
              ] as Classification[]) ||
              config.allClassifications ||
              defaultConfig.allClassifications,
            enableBB:
              (config as unknown as Record<string, unknown>)['EnableBB'] !== undefined
                ? ((config as unknown as Record<string, unknown>)['EnableBB'] as boolean)
                : config.enableBB,
            enableResource:
              (config as unknown as Record<string, unknown>)['EnableResource'] !== undefined
                ? ((config as unknown as Record<string, unknown>)['EnableResource'] as boolean)
                : config.enableResource,
            auditTypeReplacementName:
              ((config as unknown as Record<string, unknown>)[
                'AuditTypeReplacementName'
              ] as string) ||
              config.auditTypeReplacementName ||
              defaultConfig.auditTypeReplacementName,
          };
          observer.next(mappedConfig);
          observer.complete();
        },
        error: (error) => {
          observer.error(error);
        },
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
