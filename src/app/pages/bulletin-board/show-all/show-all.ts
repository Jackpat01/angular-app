import {Component, inject, OnInit} from '@angular/core';
import {DatePipe} from '@angular/common';
import {PortalConfigService} from '../../../services/portal-config';
import {TitleService} from '../../../services/title';
import {SessionStorageService} from '../../../services/session-storage-service';
import {ARUMBulletinBoards} from '../../../models/ARUMBulletinBoards';

@Component({
  imports: [DatePipe],
//  selector: 'ohfi-show-all',
  styleUrl: './show-all.css',
  templateUrl: './show-all.html',
})
export class ShowAll implements OnInit {
  private portalConfig = inject(PortalConfigService);
  protected titleService = inject(TitleService);
  private session = inject(SessionStorageService);


  myBulletinBoards: ARUMBulletinBoards[] = [];
  myBulletinBoardType = '';
  bulletinBoardTypes: string[] = [];

  ngOnInit(): void {
    this.titleService.setTitle(this.getPageTitle());

    let user = this.session.ARUMUser;

    if (user) {
      if (user.bulletinBoards) {
        this.myBulletinBoards = user.bulletinBoards;
        this.bulletinBoardTypes = [
          ...new Set(user.bulletinBoards.map(c => c.type))
        ];
      }
    }
  }

  getPageTitle(): string {
    return this.portalConfig.getWebPortalTitle() + ' - Bulletin Board';
  }

  get filteredBulletinBoards(): ARUMBulletinBoards[] {
    return [...this.myBulletinBoards]
      .filter(c => c.type?.toUpperCase() === this.myBulletinBoardType?.toUpperCase())
      .sort((a, b) => {
        const alwaysShowA = a.alwaysShow ? 1 : 0;
        const alwaysShowB = b.alwaysShow ? 1 : 0;
        if (alwaysShowB !== alwaysShowA) {
          return alwaysShowB - alwaysShowA;
        }

        if (b.order !== a.order) {
          return b.order - a.order;
        }

        const createDateA = new Date(a.createDate).getTime();
        const createDateB = new Date(b.createDate).getTime();
        if (createDateB !== createDateA) {
          return createDateB - createDateA;
        }

        const lastModA = a.lastModified ? new Date(a.lastModified).getTime() : 0;
        const lastModB = b.lastModified ? new Date(b.lastModified).getTime() : 0;
        return lastModB - lastModA;
      });
  }


}
