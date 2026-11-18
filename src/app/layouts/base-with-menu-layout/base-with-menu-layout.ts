import { Component, OnInit, inject, signal, DestroyRef, WritableSignal } from '@angular/core';
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
import { MatDialog } from '@angular/material/dialog';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

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
  session = inject(SessionStorageService);
  private uploadDownloadConfiguration = inject(UploadDownloadConfiguration);
  private logoutService = inject(LogoutService);
  private dialog = inject(MatDialog);
  private destroyRef = inject(DestroyRef);

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

  private featureAccessMap = new Map<string, WritableSignal<boolean>>([
    ['CostReport:View', this.canAccessOCRTSignal],
    ['RateSetting:View', this.canAccessRateSettingSignal],
    ['BulkProcessing:View', this.canAccessBulkProcessingViewSignal],
    ['Statlog:View', this.canAccessStatlogViewSignal],
    ['Statlog:Configuration', this.canAccessStatlogConfigurationSignal],
    ['Statlog:AllProvidersEmail', this.canAccessStatlogAllProvidersEmailSignal],
    ['OHFI:SettingsManagement', this.canAccessOHFISettingsManagementSignal],
    ['LetterAutomation:Access', this.canAccessLetterAutomationSignal],
    ['Portal:Upload', this.canAccessPortalUploadSignal],
    ['Portal:Download', this.canAccessPortalDownloadSignal],
    ['Reports:ARAP', this.canAccessReportsARAPSignal],
    ['Reports:MDS', this.canAccessReportsMDSSignal],
    ['Reports:MDSResidentListing', this.canAccessReportsMDSResidentListingSignal],
    ['Reports:SectionQ', this.canAccessReportsSectionQSignal],
    ['Reports:UploadDownload', this.canAccessReportsUploadDownloadSignal],
    ['Reports:MassUpload', this.canAccessReportsMassUploadSignal],
    [
      'Reports:CostReportSubmissionMonitoring',
      this.canAccessReportsCostReportSubmissionMonitoringSignal,
    ],
    ['Reports:CostReportDataWizard', this.canAccessReportsCostReportDataWizardSignal],
    ['CLPA:View', this.canAccessCLPAViewSignal],
  ]);

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

  get sessionClassification() {
    return this.session.classification;
  }

  get sessionIsNF() {
    return this.session.isNF;
  }

  get sessionIsICF() {
    return this.session.isICF;
  }

  get sessionIsState() {
    return this.session.isState;
  }

  sessionGetUserHasRole(role: string): boolean {
    return this.session.getUserHasRole(role);
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

    // Load all feature access permissions using the featureAccessMap
    this.featureAccessMap.forEach((signal, featureName) => {
      this.session
        .canAccessFeature(featureName)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (val) => signal.set(val),
          error: (err) => {
            console.error(`Error checking access for ${featureName}:`, err);
            signal.set(false);
          },
        });
    });
  }

  openHelpModal(): void {
    const supportEmail = this.portalConfig.getHelpDeskEmail();
    this.dialog.open(HelpModalComponent, {
      width: '450px',
      data: {
        // Replace with your Angular environment/config value
        helpDeskEmail: `<a href="mailto:${{ supportEmail }}">${{ supportEmail }}</a>`,
      },
    });
  }
  logout(): void {
    this.logoutService.executeLogout('/');
  }
}
