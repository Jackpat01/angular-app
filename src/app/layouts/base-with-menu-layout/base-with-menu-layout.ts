import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HeaderBarComponent } from '../../shared/header-bar/header-bar';
import { LogoBrandingComponent } from '../../shared/logo-branding/logo-branding';
import { FooterSectionComponent } from '../../shared/footer-section/footer-section';
import { HelpModalComponent } from '../../shared/help-modal/help-modal';
import { LoadingOverlayComponent } from '../../shared/loading-overlay/loading-overlay';
import { AdminNavigation, NavItem } from '../../shared/admin-navigation/admin-navigation';
import { TitleService } from '../../services/title';
import { AppConfigService, AppConfig } from '../../services/app-config';
import { UserService, UserInfo } from '../../services/user';
import { AdminService } from '../../services/admin';

@Component({
  imports: [
    CommonModule,
    RouterOutlet,
    HeaderBarComponent,
    LogoBrandingComponent,
    FooterSectionComponent,
    HelpModalComponent,
    LoadingOverlayComponent,
    AdminNavigation,
  ],
  selector: 'ohfi-base-with-menu-layout',
  styleUrl: './base-with-menu-layout.scss',
  templateUrl: './base-with-menu-layout.html',
})
export class BaseWithMenuLayout implements OnInit {
  appConfig: AppConfig | null = null;
  userInfo: UserInfo | null = null;
  menuItems: NavItem[] = [];
  isLoading = false;

  constructor(
    private titleService: TitleService,
    private appConfigService: AppConfigService,
    private userService: UserService,
    private adminService: AdminService
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle('Administration');
    this.appConfig = this.appConfigService.getConfig();
    this.userInfo = this.userService.getUser();
    this.menuItems = this.adminService.getMenuItems();
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
    this.userService.logout().subscribe({
      next: () => {
        this.userService.logoutLocal();
        window.location.href = '/';
      },
      error: (error) => {
        console.error('Logout failed:', error);
        this.userService.logoutLocal();
        window.location.href = '/';
      }
    });
  }
}

