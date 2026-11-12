import {Component, OnInit} from '@angular/core';
import {CommonModule, NgOptimizedImage} from '@angular/common';
import {TitleService} from '../../services/title';
import {AppConfigService} from '../../services/app-config';
import {UserInfo, UserService} from '../../services/user';
import {PortalConfigService} from '../../services/portal-config';
import {SessionStorageService} from '../../services/session-storage-service';

import {ARUMProxy_User} from '../../models/ARUMProxy_User';
import {Classification} from '../../models/Classification';
import {ARUMUserProviders} from '../../models/ARUMUserProviders';
import {ARUMRole} from '../../models/ARUMRole';

@Component({
  imports: [
    CommonModule,
    NgOptimizedImage,
  ],
  selector: 'ohfi-base-with-project-layout',
  styleUrl: './base-with-project-layout.css',
  templateUrl: './base-with-project-layout.html',
})
export class BaseWithProjectLayout implements OnInit {
  userInfo: UserInfo | null = null;
  isLoading = false;

  constructor(
    private titleService: TitleService,
    private appConfig: AppConfigService,
    private userService: UserService,
    private portalConfig: PortalConfigService,
    private storage: SessionStorageService
  ) {
  }

  get appVersion(): string {
    return this.appConfig.getAppVersion() || '';
  }

  get appName(): string {
    return this.portalConfig.getApplicationName() || '';
  }


  get currentYear(): number {
    return this.appConfig.getCurrentYear() || new Date().getFullYear();
  }
  get classReplacementTerm():string {
    return this.portalConfig.getClassificationReplacementTerm()
  }

  get userName(): string {
    let user = this.storage.getItem<ARUMProxy_User>('arumUser');

    if (user) {
      return user.email;
    }
    return 'No UserName for User';
  }

  get catalog(): string | null {
    return this.storage.getItem('initialCatalogFromSession');
  }

  get userRole(): string {
    return this.userInfo?.role || '';
  }

  get providerType(): string | null {
    var classification = this.getClassification();
    return classification == null ? null : classification.displayName
  }

  getClassification(): Classification | null {
    let classification = this.storage.getItem<Classification>(this.portalConfig.getClassificationSessionKey());
    if (classification != null)
      return classification;
    let allClassifications = this.portalConfig.getAllClassifications();
    if (allClassifications.length != 1)
      return null;
    return allClassifications[0];
  }

  get projectName(): string {
    return this.userInfo?.projectName || '';
  }

  get currentRoleName(): string {
    return this.currentRole?.roleName ?? "No Provider Selected";
  }

  get currentRole(): ARUMRole | null {
    let arumUser = this.storage.getItem<ARUMProxy_User>('arumUser')

    let arumUserProviders;
    if (arumUser == null) {
      arumUserProviders = null;
    } else {
       arumUserProviders = arumUser?.providers?.find(
        c => c.providerId === this.storage.getItem<number>('selectedProviderID') && c.providerUserRole !== ''
      ) ?? null;
    }

    let selectedProvider: ARUMUserProviders | null = arumUserProviders;

    if (!selectedProvider) {
      return null;
    }

    return arumUser?.roles?.find(
      (r: ARUMRole) => r.roleName === selectedProvider.providerUserRole
    ) ?? null;
  }

  ngOnInit(): void {
    this.titleService.setTitle('Dashboard');
    this.userInfo = this.userService.getUser();
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

