import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, NgOptimizedImage } from '@angular/common';
import { UserService } from '../../services/user';
import { PortalConfigService } from '../../services/portal-config';
import { ARUMProxy_User } from '../../models/ARUMProxy_User';
import { SessionStorageService } from '../../services/session-storage-service';
import { ARUMRole } from '../../models/ARUMRole';
import { ARUMUserProviders } from '../../models/ARUMUserProviders';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AppConfigService } from '../../services/app-config';
import { UploadDownloadConfiguration } from '../../services/upload-download-configuration';
import { LogoutService } from '../../services/logout.service';
import { HelpModalComponent } from '../../shared/help-modal/help-modal';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

@Component({
  imports: [CommonModule, RouterOutlet, RouterLink, NgOptimizedImage],
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
  private session = inject(SessionStorageService);
  private uploadDownloadConfiguration = inject(UploadDownloadConfiguration);
  private logoutService = inject(LogoutService);
  private dialog = inject(MatDialog);

  private loginMessagesSignal = signal<string[]>([]);

  // Feature access signals
  private canAccessOCRTSignal = signal<boolean>(false);
  private canAccessRateSettingSignal = signal<boolean>(false);
  private canAccessBulkProcessingViewSignal = signal<boolean>(false);
  private canAccessStatlogViewSignal = signal<boolean>(false);
  private canAccessStatlogConfigurationSignal = signal<boolean>(false);
  private canAccessStatlogAllProvidersEmailSignal = signal<boolean>(false);
  private canAccessOHFISettingsManagementSignal = signal<boolean>(false);
  private canAccessLetterAutomationSignal = signal<boolean>(false);
  private canAccessPortalUploadSignal = signal<boolean>(false);
  private canAccessPortalDownloadSignal = signal<boolean>(false);
  private canAccessReportsARAPSignal = signal<boolean>(false);
  private canAccessReportsMDSSignal = signal<boolean>(false);
  private canAccessReportsMDSResidentListingSignal = signal<boolean>(false);
  private canAccessReportsSectionQSignal = signal<boolean>(false);
  private canAccessReportsUploadDownloadSignal = signal<boolean>(false);
  private canAccessReportsMassUploadSignal = signal<boolean>(false);
  private canAccessReportsCostReportSubmissionMonitoringSignal = signal<boolean>(false);
  private canAccessReportsCostReportDataWizardSignal = signal<boolean>(false);
  private canAccessCLPAViewSignal = signal<boolean>(false);

  get loginMessage(): string[] {
    return this.loginMessagesSignal();
  }

  get isNotShowAllPage(): boolean {
    const currentPath = this.router.url.split('?')[0];
    return currentPath !== '/bulletin-board/show-all';
  }
  get canAccessOCRT(): boolean {
    return this.canAccessOCRTSignal();
  }
  get canAccessRateSetting(): boolean {
    return this.canAccessRateSettingSignal();
  }

  get canAccessBulkProcessingView(): boolean {
    return this.canAccessBulkProcessingViewSignal();
  }
  get canAccessStatlogView(): boolean {
    return this.canAccessStatlogViewSignal();
  }
  get canAccessStatlogConfiguration(): boolean {
    return this.canAccessStatlogConfigurationSignal();
  }
  get canAccessStatlogAllProvidersEmail(): boolean {
    return this.canAccessStatlogAllProvidersEmailSignal();
  }
  get canAccessOHFISettingsManagement(): boolean {
    return this.canAccessOHFISettingsManagementSignal();
  }
  get canAccessLetterAutomation(): boolean {
    return this.canAccessLetterAutomationSignal();
  }
  get canAccessPortalUpload(): boolean {
    return this.canAccessPortalUploadSignal();
  }
  get canAccessPortalDownload(): boolean {
    return this.canAccessPortalDownloadSignal();
  }
  get canAccessReportsARAP(): boolean {
    return this.canAccessReportsARAPSignal();
  }
  get canAccessReportsMDS(): boolean {
    return this.canAccessReportsMDSSignal();
  }
  get canAccessReportsMDSResidentListing(): boolean {
    return this.canAccessReportsMDSResidentListingSignal();
  }
  get canAccessReportsSectionQ(): boolean {
    return this.canAccessReportsSectionQSignal();
  }
  get canAccessReportsUploadDownload(): boolean {
    return this.canAccessReportsUploadDownloadSignal();
  }
  get canAccessReportsMassUpload(): boolean {
    return this.canAccessReportsMassUploadSignal();
  }
  get canAccessReportsCostReportSubmissionMonitoring(): boolean {
    return this.canAccessReportsCostReportSubmissionMonitoringSignal();
  }
  get canAccessReportsCostReportDataWizard(): boolean {
    return this.canAccessReportsCostReportDataWizardSignal();
  }
  get canAccessCLPAView(): boolean {
    return this.canAccessCLPAViewSignal();
  }
  get canAccessReports(): boolean {
    return (
      this.canAccessReportsARAP ||
      this.canAccessReportsUploadDownload ||
      this.canAccessReportsMassUpload ||
      this.canAccessReportsCostReportSubmissionMonitoring ||
      this.canAccessReportsCostReportDataWizard ||
      this.canAccessReportsMDS ||
      this.canAccessReportsMDSResidentListing ||
      this.canAccessReportsSectionQ
    );
  }

  get appVersion(): string {
    return this.appConfig.getAppVersion() || '';
  }

  get appName(): string {
    return this.portalConfig.getApplicationName() || '';
  }

  get classReplacementTerm(): string {
    return this.portalConfig.getClassificationReplacementTerm();
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
    const classification = this.session.classification;
    return classification === null ? null : classification.displayName;
  }

  get currentRoleName(): string {
    return this.currentRole?.roleName ?? 'No Provider Selected';
  }

  get enableUpload(): boolean {
    return this.uploadDownloadConfiguration.enableUpload();
  }
  get enableDownload(): boolean {
    return this.uploadDownloadConfiguration.enableDownload();
  }

  get currentRole(): ARUMRole | null {
    const arumUser = this.storage.getItem<ARUMProxy_User>('arumUser');

    let arumUserProviders;
    if (arumUser === null) {
      arumUserProviders = null;
    } else {
      arumUserProviders =
        arumUser?.providers?.find(
          (c) =>
            c.providerId === this.storage.getItem<number>('selectedProviderID') &&
            c.providerUserRole !== '',
        ) ?? null;
    }

    const selectedProvider: ARUMUserProviders | null = arumUserProviders;

    if (!selectedProvider) {
      return null;
    }

    return (
      arumUser?.roles?.find((r: ARUMRole) => r.roleName === selectedProvider.providerUserRole) ??
      null
    );
  }

  async ngOnInit(): Promise<void> {
    await this.portalConfig.waitForConfigLoad();
    this.loginMessagesSignal.set(this.portalConfig.retrieveLoginMessage());

    // Load all feature access permissions
    this.session
      .canAccessFeature('CostReport:View')
      .subscribe((val) => this.canAccessOCRTSignal.set(val));
    this.session
      .canAccessFeature('RateSetting:View')
      .subscribe((val) => this.canAccessRateSettingSignal.set(val));
    this.session
      .canAccessFeature('BulkProcessing:View')
      .subscribe((val) => this.canAccessBulkProcessingViewSignal.set(val));
    this.session
      .canAccessFeature('Statlog:View')
      .subscribe((val) => this.canAccessStatlogViewSignal.set(val));
    this.session
      .canAccessFeature('Statlog:Configuration')
      .subscribe((val) => this.canAccessStatlogConfigurationSignal.set(val));
    this.session
      .canAccessFeature('Statlog:AllProvidersEmail')
      .subscribe((val) => this.canAccessStatlogAllProvidersEmailSignal.set(val));
    this.session
      .canAccessFeature('OHFI:SettingsManagement')
      .subscribe((val) => this.canAccessOHFISettingsManagementSignal.set(val));
    this.session
      .canAccessFeature('LetterAutomation:Access')
      .subscribe((val) => this.canAccessLetterAutomationSignal.set(val));
    this.session
      .canAccessFeature('Portal:Upload')
      .subscribe((val) => this.canAccessPortalUploadSignal.set(val));
    this.session
      .canAccessFeature('Portal:Download')
      .subscribe((val) => this.canAccessPortalDownloadSignal.set(val));
    this.session
      .canAccessFeature('Reports:ARAP')
      .subscribe((val) => this.canAccessReportsARAPSignal.set(val));
    this.session
      .canAccessFeature('Reports:MDS')
      .subscribe((val) => this.canAccessReportsMDSSignal.set(val));
    this.session
      .canAccessFeature('Reports:MDSResidentListing')
      .subscribe((val) => this.canAccessReportsMDSResidentListingSignal.set(val));
    this.session
      .canAccessFeature('Reports:SectionQ')
      .subscribe((val) => this.canAccessReportsSectionQSignal.set(val));
    this.session
      .canAccessFeature('Reports:UploadDownload')
      .subscribe((val) => this.canAccessReportsUploadDownloadSignal.set(val));
    this.session
      .canAccessFeature('Reports:MassUpload')
      .subscribe((val) => this.canAccessReportsMassUploadSignal.set(val));
    this.session
      .canAccessFeature('Reports:CostReportSubmissionMonitoring')
      .subscribe((val) => this.canAccessReportsCostReportSubmissionMonitoringSignal.set(val));
    this.session
      .canAccessFeature('Reports:CostReportDataWizard')
      .subscribe((val) => this.canAccessReportsCostReportDataWizardSignal.set(val));
    this.session
      .canAccessFeature('CLPA:View')
      .subscribe((val) => this.canAccessCLPAViewSignal.set(val));
  }

  openHelpModal(): void {
    let supportEmail = this.portalConfig.getHelpDeskEmail();
    this.dialog.open(HelpModalComponent, {
      width: '450px',
      data: {
        // Replace with your Angular environment/config value
        helpDeskEmail: `<a href="mailto:${{supportEmail}}">${{supportEmail}}</a>`
      }
    });
  }
  logout(): void {
    this.logoutService.executeLogout('/');
  }
}
