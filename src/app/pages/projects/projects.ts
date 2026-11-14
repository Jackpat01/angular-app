import {Component, computed, inject, OnInit, Signal} from '@angular/core';
import {TempDataService} from '../../services/temp-data';
import {Classification} from '../../models/Classification';
import {PortalConfigService} from '../../services/portal-config';
import {Router, RouterLink} from '@angular/router';
import {FunctionsService} from '../../services/functions-service';
import {SessionStorageService} from '../../services/session-storage-service';
import {ARUMProxy_User} from '../../models/ARUMProxy_User';
import {SESSION_KEYS_OCRT} from '../../models/session-keys.constants';
import {TitleService} from '../../services/title';

@Component({
  imports: [RouterLink],
  selector: 'ohfi-projects',
  styleUrl: './projects.scss',
  templateUrl: './projects.html',
})
export class Projects implements OnInit {
  private titleService = inject(TitleService);
  private tempData = inject(TempDataService);
  private portalConfig = inject(PortalConfigService);
  private functions = inject(FunctionsService);
  private session = inject(SessionStorageService);
  private router = inject(Router);

  ngOnInit(): void {
    this.titleService.setTitle('Projects');

    let arumUser = this.session.getItem<ARUMProxy_User>('armUser');
    if (arumUser == null) {
      if (!this.session.getItem<boolean>('IsSSO')) {
        this.router.navigate(['/login']);
      } else {
        this.router.navigate(['/sso/error']);
      }
    }

    this.session.setItem('selectedProviderID', 0);
    this.session.removeItem('AppNameFromSession');
    this.session.removeItem('InitialCatalogFromSession');
    this.session.removeItem('ProviderDictionary');
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
    this.session.removeItem('InternalIdMapping');
    this.session.removeItem(this.portalConfig.getClassificationSessionKey());
  }

  get classifications(): Signal<Classification[]> {
    let classifications = this.portalConfig.getAllClassifications();

    let arumValidations = this.functions.getValidations();

    if (arumValidations?.lstARUMUsers?.length) {
      classifications = classifications.filter(singClassification =>
        arumValidations.lstARUMUsers.some(c => c.token === singClassification.arumToken)
      );
    }

    return computed(() =>
      [...classifications].sort((a, b) =>
        a.displayName.localeCompare(b.displayName)
      )
    );
  }
}
