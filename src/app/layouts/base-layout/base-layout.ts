import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeaderBarComponent } from '../../shared/header-bar/header-bar';
import { LogoBrandingComponent } from '../../shared/logo-branding/logo-branding';
import { ContentPanelComponent } from '../../shared/content-panel/content-panel';
import { FooterSectionComponent } from '../../shared/footer-section/footer-section';
import { TitleService } from '../../services/title';
import { AppConfigService, AppConfig } from '../../services/app-config';

@Component({
  imports: [CommonModule, HeaderBarComponent, LogoBrandingComponent, ContentPanelComponent, FooterSectionComponent],
  selector: 'ohfi-base-layout',
  styleUrl: './base-layout.scss',
  templateUrl: './base-layout.html',
})
export class BaseLayout implements OnInit {
  appConfig: AppConfig | null = null;

  constructor(
    private titleService: TitleService,
    private appConfigService: AppConfigService
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle('Welcome');
    this.appConfig = this.appConfigService.getConfig();
  }

  get appVersion(): string {
    return this.appConfig?.appVersion || '';
  }

  get currentYear(): number {
    return this.appConfig?.currentYear || new Date().getFullYear();
  }
}

