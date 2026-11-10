import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeaderBarComponent } from '../../shared/header-bar/header-bar';
import { LogoBrandingComponent } from '../../shared/logo-branding/logo-branding';
import { ContentPanelComponent } from '../../shared/content-panel/content-panel';
import { FooterSectionComponent } from '../../shared/footer-section/footer-section';
import { HelpModalComponent } from '../../shared/help-modal/help-modal';
import { LoadingOverlayComponent } from '../../shared/loading-overlay/loading-overlay';
import { TitleService } from '../../services/title';
import { AppConfigService, AppConfig } from '../../services/app-config';
import { UserService, UserInfo } from '../../services/user';

@Component({
  imports: [
    CommonModule,
    HeaderBarComponent,
    LogoBrandingComponent,
    ContentPanelComponent,
    FooterSectionComponent,
    HelpModalComponent,
    LoadingOverlayComponent,
  ],
  selector: 'app-base-with-project-layout',
  styleUrl: './base-with-project-layout.scss',
  templateUrl: './base-with-project-layout.html',
})
export class BaseWithProjectLayout implements OnInit {
  appConfig: AppConfig | null = null;
  userInfo: UserInfo | null = null;
  isLoading = false;

  constructor(
    private titleService: TitleService,
    private appConfigService: AppConfigService,
    private userService: UserService
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle('Dashboard');
    this.appConfig = this.appConfigService.getConfig();
    this.userInfo = this.userService.getUser();
  }

  get appVersion(): string {
    return this.appConfig?.appVersion || '';
  }

  get currentYear(): number {
    return this.appConfig?.currentYear || new Date().getFullYear();
  }

  get userEmail(): string {
    return this.userInfo?.email || '';
  }

  get userRole(): string {
    return this.userInfo?.role || '';
  }

  get providerType(): string {
    return this.userInfo?.providerType || '';
  }

  get projectName(): string {
    return this.userInfo?.projectName || '';
  }

  logout(): void {
    console.log('Logging out...');
    this.userService.logout();
    window.location.href = '/';
  }
}

