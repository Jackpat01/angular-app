import {Component, inject, OnInit, signal} from '@angular/core';
import {Router} from '@angular/router';
import {TempDataService} from '../../core/services/temp-data';
import {TitleService} from '../../core/services/title';
import {PortalConfigService} from '../../core/services/portal-config';
import {SessionStorageService} from '../../core/services/session-storage-service';


@Component({
  imports: [],
  styleUrl: './secure.css',
  templateUrl: './secure.html',
})
export class Secure implements OnInit {
  private router = inject(Router);
  private tempData = inject(TempDataService);
  private titleService = inject(TitleService);
  private portalConfig = inject(PortalConfigService);
  private session = inject(SessionStorageService);


  protected message = signal<string | null>(null);

  ngOnInit(): void {
    this.session.removeItem('selectedProviderID');

    let myUser = this.session.ARUMUser;

    if (myUser) {
      this.titleService.setTitle(this.portalConfig.getWebPortalTitle() + ' - Secure - Index');

      const data = this.tempData.getTempData('message');
      const value = typeof data?.['message'] === 'string' ? data['message'] : null;
      this.message.set(value);
    } else {
      this.router.navigate(['/login']);
    }
  }
}
