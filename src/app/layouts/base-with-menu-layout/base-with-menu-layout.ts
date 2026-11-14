import {Component, OnInit, inject, signal} from '@angular/core';
import {CommonModule, NgOptimizedImage} from '@angular/common';
import {TitleService} from '../../services/title';
import {UserService} from '../../services/user';
import {PortalConfigService} from '../../services/portal-config';
import {ARUMProxy_User} from '../../models/ARUMProxy_User';
import {SessionStorageService} from '../../services/session-storage-service';
import {ARUMRole} from '../../models/ARUMRole';
import {ARUMUserProviders} from '../../models/ARUMUserProviders';
import {Classification} from '../../models/Classification';
import {Router, RouterLink, RouterOutlet} from '@angular/router';
import {AppConfigService} from '../../services/app-config';

@Component({
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    NgOptimizedImage,
  ],
  selector: 'ohfi-base-with-menu-layout',
  styleUrl: './base-with-menu-layout.css',
  templateUrl: './base-with-menu-layout.html',
})
export class BaseWithMenuLayout implements OnInit {
  private router = inject(Router);
  private userService = inject(UserService);
  private portalConfig = inject(PortalConfigService);
  private storage = inject(SessionStorageService);
  private appConfig = inject(AppConfigService);
  private session=inject(SessionStorageService);

  private _loginMessages: string[] = [];

  get loginMessage(): string[] {
    return this._loginMessages;
  }

  get isNotShowAllPage(): boolean {
    const currentPath = this.router.url.split('?')[0];
    return currentPath !== '/bulletin-board/show-all';
  }

  get canAccessBulkProcessingView(): boolean {
    return this.session.canAccessFeature('BulkProcessing:View');
  }
  get canAccessStatlogView(): boolean {
    return this.session.canAccessFeature('Statlog:View');
  }
  get canAccessStatlogConfiguration(): boolean {
    return this.session.canAccessFeature('Statlog:Configuration');
  }
  get canAccessStatlogAllProvidersEmail(): boolean {
    return this.session.canAccessFeature('Statlog:AllProvidersEmail');
  }
  get canAccessOHFISettingsManagement(): boolean {
    return this.session.canAccessFeature('OHFI:SettingsManagement');
  }
  get canAccessLetterAutomation(): boolean {
    return this.session.canAccessFeature("LetterAutomation:Access");
  }
  get canAccessPortalUpload(): boolean {
    return this.session.canAccessFeature("Portal:Upload");
  }
  get canAccessPortalDownload(): boolean {
    return this.session.canAccessFeature("Portal:Download");
  }
  get canAccessReportsARAP(): boolean {
    return this.session.canAccessFeature("Reports:ARAP");
  }
  get canAccessReportsMDS(): boolean {
    return this.session.canAccessFeature("Reports:MDS");
  }
  get canAccessReportsMDSResidentListing(): boolean {
    return this.session.canAccessFeature("Reports:MDSResidentListing");
  }
  get canAccessReportsSectionQ(): boolean {
    return this.session.canAccessFeature("Reports:SectionQ");
  }
  get canAccessReportsUploadDownload(): boolean {
    return this.session.canAccessFeature("Reports:UploadDownload");
  }
  get canAccessReportsMassUpload(): boolean {
    return this.session.canAccessFeature("Reports:MassUpload");
  }
  get canAccessReportsCostReportSubmissionMonitoring(): boolean {
    return this.session.canAccessFeature("Reports:CostReportSubmissionMonitoring");
  }
  get canAccessReportsCostReportDataWizard(): boolean {
    return this.session.canAccessFeature("Reports:CostReportDataWizard");
  }
  get canAccessCLPAView(): boolean {
    return this.session.canAccessFeature("CLPA:View");
  }
  get canAccessReports(): boolean {
    return this.canAccessReportsARAP
      || this.canAccessReportsUploadDownload
      || this.canAccessReportsMassUpload
      || this.canAccessReportsCostReportSubmissionMonitoring
      || this.canAccessReportsCostReportDataWizard
      || this.canAccessReportsMDS
      || this.canAccessReportsMDSResidentListing
      || this.canAccessReportsSectionQ;
  }

  get appVersion(): string {
    return this.appConfig.getAppVersion() || '';
  }

  get appName(): string {
    return this.portalConfig.getApplicationName() || '';
  }

  get classReplacementTerm(): string {
    return this.portalConfig.getClassificationReplacementTerm()
  }

  get currentYear(): number {
    return this.appConfig.getCurrentYear() || new Date().getFullYear();
  }


  get userName(): string {
    const user = this.storage.getItem<ARUMProxy_User>('arumUser');

    if (user) {
      return user.email.toUpperCase();
    }
    return 'No UserName for User';
  }

  get catalog(): string | null {
    return this.storage.getItem('initialCatalogFromSession');
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
    this.portalConfig.refreshConfig();
    this._loginMessages = await this.portalConfig.retrieveLoginMessage();

  }

  isHelpModalOpen = signal(false);

  openHelpModal(): void {
    this.isHelpModalOpen.set(true);
    // this.dialog.open(HelpNumberModalComponent, { disableClose: true });
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

