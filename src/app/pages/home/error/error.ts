import {Component, OnInit, inject} from '@angular/core';
import {TitleService} from '../../../services/title';
import {PortalConfigService} from '../../../services/portal-config';

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
    // Wait for portal config to load before setting title
    this.portalConfig.waitForConfigLoad().then(() => {
      this.titleService.setTitle(this.portalConfig.getWebPortalTitle() + " - Error");
    }).catch(error => {
      console.error('Error loading portal config:', error);
      this.titleService.setTitle('Error');
    });
  }
}

