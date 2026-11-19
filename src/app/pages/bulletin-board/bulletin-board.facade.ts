import {computed, inject, Injectable, signal} from '@angular/core';
import {ARUMBulletinBoards} from '../../models/ARUMBulletinBoards';
import {PortalConfigService} from '../../services/portal-config';
import {SessionStorageService} from '../../services/session-storage-service';
import {TitleService} from '../../services/title';

/**
 * Facade for the Bulletin Board feature.
 * Centralises state (signals), derived state (computed), filtering logic,
 * and title management so that page components stay thin.
 */
@Injectable({providedIn: 'root'})
export class BulletinBoardFacade {
  private readonly session = inject(SessionStorageService);
  private readonly portalConfig = inject(PortalConfigService);
  private readonly titleService = inject(TitleService);

  private readonly _boards = signal<ARUMBulletinBoards[]>([]);

  readonly boards = this._boards.asReadonly();
  readonly boardTypes = computed(() => [
    ...new Set(this._boards().map((b) => b.type)),
  ]);

  loadFromSession(): void {
    const user = this.session.ARUMUser;
    if (user?.bulletinBoards) {
      this._boards.set(user.bulletinBoards);
    }
  }

  getFilteredBoards(type: string): ARUMBulletinBoards[] {
    return [...this._boards()]
      .filter((b) => b.type?.toUpperCase() === type?.toUpperCase())
      .sort((a, b) => {
        const pinA = a.alwaysShow ? 1 : 0;
        const pinB = b.alwaysShow ? 1 : 0;
        if (pinB !== pinA) return pinB - pinA;
        if (b.order !== a.order) return b.order - a.order;
        const dateA = new Date(a.createDate).getTime();
        const dateB = new Date(b.createDate).getTime();
        if (dateB !== dateA) return dateB - dateA;
        const modA = a.lastModified ? new Date(a.lastModified).getTime() : 0;
        const modB = b.lastModified ? new Date(b.lastModified).getTime() : 0;
        return modB - modA;
      });
  }

  setPageTitle(): void {
    this.titleService.setTitle(
      this.portalConfig.getWebPortalTitle() + ' - Bulletin Board',
    );
  }
}
