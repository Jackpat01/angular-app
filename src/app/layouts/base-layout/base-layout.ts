import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppConfigService } from '../../services/app-config';
import { RouterLink, RouterOutlet } from '@angular/router';
import { PortalConfigService } from '../../services/portal-config';

@Component({
  imports: [CommonModule, RouterOutlet, RouterLink],
//  selector: 'ohfi-base-layout',
  styleUrl: './base-layout.css',
  templateUrl: './base-layout.html',
})
export class BaseLayout implements OnInit {
  protected readonly portalConfig = inject(PortalConfigService);
  private appConfig = inject(AppConfigService);
  private loginMessagesSignal = signal<string[]>([]);

  get loginMessage(): string[] {
    return this.loginMessagesSignal();
  }

  get appVersion(): string {
    return this.appConfig.getAppVersion() || '';
  }

  get currentYear(): number {
    return this.appConfig.getCurrentYear() || new Date().getFullYear();
  }

  async ngOnInit(): Promise<void> {
    await this.portalConfig.waitForConfigLoad();
    this.loginMessagesSignal.set(this.portalConfig.retrieveLoginMessage());
  }
}
