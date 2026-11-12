import {Component, OnInit} from '@angular/core';
import {TitleService} from '../../../services/title';
import {PortalConfigService} from '../../../services/portal-config';

@Component({
  imports: [],
  selector: 'ohfi-error',
  styleUrl: './error.css',
  templateUrl: './error.html',
})
export class ErrorPage implements OnInit {
  constructor(
    private titleService: TitleService,
    private portalConfig: PortalConfigService
  ) {
  }

  ngOnInit(): void {
    this.titleService.setTitle(this.portalConfig.getWebPortalTitle() + " - Error");
  }
}

