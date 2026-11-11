import {Component, inject, OnInit, signal} from '@angular/core';
import { CommonModule } from '@angular/common';
import { TitleService } from '../../services/title';
import { AppConfigService, AppConfig } from '../../services/app-config';
import {RouterLink, RouterOutlet} from '@angular/router';
import {PortalConfigService} from '../../services/portal-config';

@Component({
  imports: [CommonModule, RouterOutlet, RouterLink],
  selector: 'ohfi-base-layout',
  styleUrl: './base-layout.css',
  templateUrl: './base-layout.html',
})
export class BaseLayout implements OnInit {
  appConfig: AppConfig | null = null;
  protected readonly portalConfig = inject(PortalConfigService);

  constructor(
    private titleService: TitleService,
    private appConfigService: AppConfigService
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle('Welcome');
    this.appConfig = this.appConfigService.getConfig();
    this.portalConfig.refreshConfig();
  }

  get appVersion(): string {
    return this.appConfig?.appVersion || '';
  }

  get currentYear(): number {
    return this.appConfig?.currentYear || new Date().getFullYear();
  }
}

