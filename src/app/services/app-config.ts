import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

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
  private configSubject = new BehaviorSubject<AppConfig>({
    appVersion: '1.0.0',
    currentYear: new Date().getFullYear(),
    appName: 'Web Portal',
    supportEmail: 'support@example.com',
  });

  config$ = this.configSubject.asObservable();

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
