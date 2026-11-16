import {Injectable, inject} from '@angular/core';
import {ApiService} from './api';
import {Observable} from 'rxjs';
import {BaseConfigService} from './base-config.service';

export interface AppConfig {
  appVersion: string;
  currentYear: number;
}

@Injectable({
  providedIn: 'root',
})
export class AppConfigService extends BaseConfigService<AppConfig> {
  private apiService = inject(ApiService);

  constructor() {
    super({
      defaultValue: {
        appVersion: '0.0.2',
        currentYear: new Date().getFullYear()
      },
      endpoint: '/appConfiguration/config'
    });
    this.loadConfig();
  }

  protected loadFromApi(): Observable<AppConfig> {
    return this.apiService.get<AppConfig>(this.endpoint);
  }

  getAppVersion(): string {
    return this.getConfig().appVersion;
  }

  getCurrentYear(): number {
    return this.getConfig().currentYear;
  }
}


