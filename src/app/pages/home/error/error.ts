import {Component, OnInit, inject} from '@angular/core';
import {TitleService} from '../../../services/title';
import {PortalConfigService} from '../../../services/portal-config';

@Component({
  imports: [],
  selector: 'ohfi-error',
  styleUrl: './error.css',
  templateUrl: './error.html',
})
export class ErrorPage implements OnInit {
  private titleService = inject(TitleService);
  private portalConfig = inject(PortalConfigService);

  ngOnInit(): void {
    this.titleService.setTitle(this.portalConfig.getWebPortalTitle() + " - Error");
  }
}

