import {inject, Injectable} from '@angular/core';
import {Observable} from 'rxjs';
import {map} from 'rxjs/operators';
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
      helpDeskEmail: 'OMESCR.RSwebportal@mslc.com'
    };

    super({
      defaultValue: defaultConfig,
      endpoint: '/portalConfiguration/get-config',
    });
    this.loadConfig();
  }

  protected loadFromApi(): Observable<PortalConfig> {
    const defaultConfig = this.defaultValue;
    return this.apiService.get<PortalConfig>(this.endpoint).pipe(
      map((config) => {
        const c = config as unknown as Record<string, unknown>;
        return {
          webPortalTitle:
            (c['WebPortalTitle'] as string) || config.webPortalTitle || defaultConfig.webPortalTitle,
          application:
            (c['ApplicationName'] as string) || config.application || defaultConfig.application,
          loginMessage:
            (c['loginMessage'] as string[]) ||
            (c['loginmessage'] as string[]) ||
            config.loginMessage ||
            defaultConfig.loginMessage,
          classificationSessionKey:
            (c['ClassificationSessionKey'] as string) ||
            config.classificationSessionKey ||
            defaultConfig.classificationSessionKey,
          classificationReplacementTerm:
            (c['ClassificationReplacementTerm'] as string) ||
            config.classificationReplacementTerm ||
            defaultConfig.classificationReplacementTerm,
          allClassifications:
            (c['AllClassifications'] as Classification[]) ||
            config.allClassifications ||
            defaultConfig.allClassifications,
          enableBB:
            c['EnableBB'] !== undefined ? (c['EnableBB'] as boolean) : config.enableBB,
          enableResource:
            c['EnableResource'] !== undefined
              ? (c['EnableResource'] as boolean)
              : config.enableResource,
          auditTypeReplacementName:
            (c['AuditTypeReplacementName'] as string) ||
            config.auditTypeReplacementName ||
            defaultConfig.auditTypeReplacementName,
          helpDeskEmail:
            (c['HelpDeskEmail'] as string) ||
            config.helpDeskEmail ||
            defaultConfig.helpDeskEmail,
        };
      }),
    );
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

  getHelpDeskEmail(): string {
    return this.getConfig().helpDeskEmail;
  }

  getClassificationSessionKey(): string {
    return this.getConfig().classificationSessionKey;
  }
}
