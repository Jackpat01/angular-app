import {Component, OnInit, inject} from '@angular/core';
import {TitleService} from '../../../../core/services/title';
import {PortalConfigService} from '../../../../core/services/portal-config';

@Component({
  imports: [],
//  selector: 'ohfi-error',
  styleUrl: './error.css',
  templateUrl: './error.html',
})
export class ErrorPage implements OnInit {
  private titleService = inject(TitleService);
  private portalConfig = inject(PortalConfigService);

  ngOnInit(): void {
    this.portalConfig.waitForConfigLoad().then(() => {
      this.titleService.setTitle(this.portalConfig.getWebPortalTitle() + " - Error");
    }).catch(error => {
      console.error('Error loading portal config:', error);
      this.titleService.setTitle('Error');
    });
  }
}
