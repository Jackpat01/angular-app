import {Component, computed, inject, OnInit, Signal} from '@angular/core';
import {TempDataService} from '../../services/temp-data';
import {Classification} from '../../models/Classification';
import {PortalConfigService} from '../../services/portal-config';
import {Router, RouterLink} from '@angular/router';
import {FunctionsService} from '../../services/functions-service';
import {SessionStorageService} from '../../services/session-storage-service';
import {ARUMProxy_User} from '../../models/ARUMProxy_User';
@Component({
  imports: [RouterLink],
  selector: 'ohfi-projects',
  styleUrl: './projects.scss',
  templateUrl: './projects.html',
})
export class Projects implements OnInit {
  tempData = inject(TempDataService);
  portalConfig = inject(PortalConfigService);
  functions=inject(FunctionsService);
  session=inject(SessionStorageService);
  private router = inject(Router);

  ngOnInit(): void {
    let arumUser = this.session.getItem<ARUMProxy_User>('armUser');
    if (arumUser == null)
    {
      if (!OHFISession.IsSSO)
      {
        this.router.navigate(['/login']);
      }
      else
      {
        this.router.navigate(['/sso/error']);      }
    }

    // reset any session variables that would cause issues from any previous project selection.
    OHFISession.ResetForNewProject();

    this.session.setItem(SESSION_KEYS_OCRT.SELECTED_COST_REPORT,null);
    this.session.setItem(SESSION_KEYS_OCRT.PROVIDER_DATA,null);
    this.session.setItem(SESSION_KEYS_OCRT.SCHEDULE_GRID_MASK,null);
    this.session.setItem(SESSION_KEYS_OCRT.SCHED_PAGE_COUNT_LAST,null);
    this.session.setItem(SESSION_KEYS_OCRT.SCHED_PAGE_COUNT_FIRST,null);
    this.session.setItem(SESSION_KEYS_OCRT.SCHED_PAGE_COUNTER,null);
    this.session.setItem(SESSION_KEYS_OCRT.SCHED_LAST,null);
    this.session.setItem(SESSION_KEYS_OCRT.SELECTED_TAB,null);
    this.session.setItem(SESSION_KEYS_OCRT.SCHEDULE_VALUES_UPDATED,null);
    this.session.setItem(SESSION_KEYS_OCRT.SCHEDULE_EDITS_REQUIRED,null);
    this.session.setItem(SESSION_KEYS_OCRT.SELECTED_SCHEDULE,null);
    this.session.setItem(SESSION_KEYS_OCRT.USE_LOCAL_CALCULATIONS,true);
    ClientMapping.ClearAllClientIdMappings();
    this.session.setItem(this.portalConfig.getClassificationSessionKey(),null);
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
