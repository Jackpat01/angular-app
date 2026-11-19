import {Component, OnInit, signal} from '@angular/core';

@Component({
  imports: [],
  selector: 'ohfi-stat-log',
  styleUrl: './stat-log.css',
  templateUrl: './stat-log.html',
})
export class StatLog implements OnInit{
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
