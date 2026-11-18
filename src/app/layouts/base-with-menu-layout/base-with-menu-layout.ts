import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
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
    console.log('BaseWithMenuLayout ngOnInit starting');
    await this.portalConfig.waitForConfigLoad();
    this.loginMessagesSignal.set(this.portalConfig.retrieveLoginMessage());

    console.log('session.classification:', this.session.classification);
    console.log('session.isNF:', this.session.isNF);
    console.log('session.isICF:', this.session.isICF);

    console.log('About to load feature access permissions...');
    // Load all feature access permissions
    try {
      console.log('Creating CostReport:View subscription...');
      this.session
        .canAccessFeature('CostReport:View')
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (val) => {
            console.log('CostReport:View =', val);
            this.canAccessOCRTSignal.set(val);
          },
          error: (err) => {
            console.error('CostReport:View error:', err);
            this.canAccessOCRTSignal.set(false);
          },
        });
      console.log('Created CostReport:View subscription');
    } catch (err) {
      console.error('Error creating CostReport:View subscription:', err);
    }
    try {
      console.log('Creating RateSetting:View subscription...');
      this.session
        .canAccessFeature('RateSetting:View')
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (val) => {
            console.log('RateSetting:View =', val);
            this.canAccessRateSettingSignal.set(val);
          },
          error: (err) => {
            console.error('RateSetting:View error:', err);
            this.canAccessRateSettingSignal.set(false);
          },
        });
    } catch (err) {
      console.error('Error creating RateSetting:View subscription:', err);
    }
    this.session
      .canAccessFeature('BulkProcessing:View')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (val) => {
          console.log('BulkProcessing:View =', val);
          this.canAccessBulkProcessingViewSignal.set(val);
        },
        error: (err) => {
          console.error('BulkProcessing:View error:', err);
          this.canAccessBulkProcessingViewSignal.set(false);
        },
      });
    this.session
      .canAccessFeature('Statlog:View')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (val) => {
          console.log('Statlog:View =', val);
          this.canAccessStatlogViewSignal.set(val);
        },
        error: (err) => {
          console.error('Statlog:View error:', err);
          this.canAccessStatlogViewSignal.set(false);
        },
      });
    this.session
      .canAccessFeature('Statlog:Configuration')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => this.canAccessStatlogConfigurationSignal.set(val));
    this.session
      .canAccessFeature('Statlog:AllProvidersEmail')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => this.canAccessStatlogAllProvidersEmailSignal.set(val));
    this.session
      .canAccessFeature('OHFI:SettingsManagement')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => this.canAccessOHFISettingsManagementSignal.set(val));
    this.session
      .canAccessFeature('LetterAutomation:Access')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => this.canAccessLetterAutomationSignal.set(val));
    this.session
      .canAccessFeature('Portal:Upload')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => this.canAccessPortalUploadSignal.set(val));
    this.session
      .canAccessFeature('Portal:Download')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => this.canAccessPortalDownloadSignal.set(val));
    this.session
      .canAccessFeature('Reports:ARAP')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => this.canAccessReportsARAPSignal.set(val));
    this.session
      .canAccessFeature('Reports:MDS')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => this.canAccessReportsMDSSignal.set(val));
    this.session
      .canAccessFeature('Reports:MDSResidentListing')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => this.canAccessReportsMDSResidentListingSignal.set(val));
    this.session
      .canAccessFeature('Reports:SectionQ')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => this.canAccessReportsSectionQSignal.set(val));
    this.session
      .canAccessFeature('Reports:UploadDownload')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => this.canAccessReportsUploadDownloadSignal.set(val));
    this.session
      .canAccessFeature('Reports:MassUpload')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => this.canAccessReportsMassUploadSignal.set(val));
    this.session
      .canAccessFeature('Reports:CostReportSubmissionMonitoring')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => this.canAccessReportsCostReportSubmissionMonitoringSignal.set(val));
    this.session
      .canAccessFeature('Reports:CostReportDataWizard')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => this.canAccessReportsCostReportDataWizardSignal.set(val));
    this.session
      .canAccessFeature('CLPA:View')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => this.canAccessCLPAViewSignal.set(val));
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
