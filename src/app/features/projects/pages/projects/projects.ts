import { Component, inject, OnInit, Signal, signal, ChangeDetectionStrategy } from '@angular/core';
import { TempDataService } from '../../../../core/services/temp-data';
import { Classification } from '../../../../core/models/Classification';
import { PortalConfigService } from '../../../../core/services/portal-config';
import { Router, RouterLink } from '@angular/router';
import { FunctionsService } from '../../../../core/services/functions-service';
import { SessionStorageService } from '../../../../core/services/session-storage-service';
import { ARUMProxy_User } from '../../../../core/models/ARUMProxy_User';
import { SESSION_KEYS_OCRT } from '../../../../core/models/session-keys.constants';
import { TitleService } from '../../../../core/services/title';
import { BaseReactiveComponent } from '../../../../shared/base/base-reactive.component';
import { takeUntil } from 'rxjs/operators';

@Component({
  imports: [RouterLink],
//  selector: 'ohfi-projects',
  styleUrl: './projects.css',
  templateUrl: './projects.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Projects extends BaseReactiveComponent implements OnInit {
  private titleService = inject(TitleService);
  private tempData = inject(TempDataService);
  private portalConfig = inject(PortalConfigService);
  private functions = inject(FunctionsService);
  private session = inject(SessionStorageService);
  private router = inject(Router);

  private classificationSignal = signal<Classification[]>([]);

  ngOnInit(): void {
    this.titleService.setTitle('Projects');

    const arumUser = this.session.getItem<ARUMProxy_User>('arumUser');
    if (arumUser == null) {
      if (!this.session.getItem<boolean>('isSSO')) {
        this.router.navigate(['/login']);
      } else {
        this.router.navigate(['/sso/error']);
      }
    }

    this.portalConfig
      .waitForConfigLoad()
      .then(() => {
        this.updateClassifications();
      })
      .catch((error) => {
        console.error('Error loading portal config:', error);
      });

    this.portalConfig.config$.pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.updateClassifications();
    });

    this.session.setItem('selectedProviderID', 0);
    this.session.removeItem('appNameFromSession');
    this.session.removeItem('initialCatalogFromSession');
    this.session.removeItem('providerDictionary');
    this.session.removeItem(SESSION_KEYS_OCRT.SELECTED_COST_REPORT);
    this.session.removeItem(SESSION_KEYS_OCRT.PROVIDER_DATA);
    this.session.removeItem(SESSION_KEYS_OCRT.SCHEDULE_GRID_MASK);
    this.session.removeItem(SESSION_KEYS_OCRT.SCHED_PAGE_COUNT_LAST);
    this.session.removeItem(SESSION_KEYS_OCRT.SCHED_PAGE_COUNT_FIRST);
    this.session.removeItem(SESSION_KEYS_OCRT.SCHED_PAGE_COUNTER);
    this.session.removeItem(SESSION_KEYS_OCRT.SCHED_LAST);
    this.session.removeItem(SESSION_KEYS_OCRT.SELECTED_TAB);
    this.session.removeItem(SESSION_KEYS_OCRT.SCHEDULE_VALUES_UPDATED);
    this.session.removeItem(SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED);
    this.session.removeItem(SESSION_KEYS_OCRT.SELECTED_SCHEDULE);
    this.session.setItem(SESSION_KEYS_OCRT.USE_LOCAL_CALCULATIONS, true);
    this.session.removeItem('internalIdMapping');
    this.session.removeItem(this.portalConfig.getClassificationSessionKey());
  }

  private updateClassifications(): void {
    let classifications = this.portalConfig.getAllClassifications();

    const arumValidations = this.functions.getValidations();

    if (arumValidations?.lstARUMUsers?.length) {
      classifications = classifications.filter((singClassification) =>
        arumValidations.lstARUMUsers.some((c) => c.token === singClassification.arumToken),
      );
    } else {
      classifications = [];
    }

    classifications = classifications.sort((a, b) => a.displayName.localeCompare(b.displayName));

    this.classificationSignal.set(classifications);
  }

  get classifications(): Signal<Classification[]> {
    return this.classificationSignal;
  }
}
