import {Component, OnInit, inject} from '@angular/core';
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
import {RouterLink, RouterOutlet} from '@angular/router';

@Component({
  imports: [
    CommonModule,
    NgOptimizedImage,
    RouterOutlet,
    RouterLink,
  ],
  selector: 'ohfi-base-with-project-layout',
  styleUrl: './base-with-project-layout.css',
  templateUrl: './base-with-project-layout.html',
})
export class BaseWithProjectLayout implements OnInit {
  private titleService = inject(TitleService);
  private appConfig = inject(AppConfigService);
  private userService = inject(UserService);
  private portalConfig = inject(PortalConfigService);
  private storage = inject(SessionStorageService);
  userInfo: UserInfo | null = null;

  private _loginMessages: string[] = [];

  get loginMessage(): string[] {
    return this._loginMessages;
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
    const user = this.storage.getItem<ARUMProxy_User>('arumUser');

    if (user) {
      return user.email.toLowerCase();
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
    const classification = this.classification;
    return classification === null ? null : classification.displayName
  }

  get classification(): Classification | null {
    const classification = this.storage.getItem<Classification>(this.portalConfig.getClassificationSessionKey());
    if (classification !== null)
      return classification;
    const allClassifications = this.portalConfig.getAllClassifications();
    if (allClassifications.length !== 1)
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
    const arumUser = this.storage.getItem<ARUMProxy_User>('arumUser')

    let arumUserProviders;
    if (arumUser === null) {
      arumUserProviders = null;
    } else {
       arumUserProviders = arumUser?.providers?.find(
        c => c.providerId === this.storage.getItem<number>('selectedProviderID') && c.providerUserRole !== ''
      ) ?? null;
    }

    const selectedProvider: ARUMUserProviders | null = arumUserProviders;

    if (!selectedProvider) {
      return null;
    }

    return arumUser?.roles?.find(
      (r: ARUMRole) => r.roleName === selectedProvider.providerUserRole
    ) ?? null;
  }

  async ngOnInit(): Promise<void> {
    this.userInfo = this.userService.getUser();
    this._loginMessages = await this.portalConfig.retrieveLoginMessage();
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

