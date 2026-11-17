import { Injectable, inject } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router } from '@angular/router';
import { SessionStorageService } from '../services/session-storage-service';
import { ProjectService } from '../services/project-service';
import { PortalConfigService } from '../services/portal-config';
import { TempDataService } from '../services/temp-data';
import { ARUMProxy_User } from '../models/ARUMProxy_User';
import { Classification } from '../models/Classification';
import {firstValueFrom} from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ProjectSelectionGuard implements CanActivate {
  private session = inject(SessionStorageService);
  private project = inject(ProjectService);
  private portalConfig = inject(PortalConfigService);
  private tempData = inject(TempDataService);
  private router = inject(Router);

  async canActivate(route: ActivatedRouteSnapshot): Promise<boolean> {
    const classificationId = route.queryParams['cID'];
    const myUser = this.session.getItem<ARUMProxy_User>('arumUser');

    if (!myUser) {
      this.router.navigate(['/login']);
      return false;
    }

    if (!classificationId) {
      this.router.navigate(['/projects']);
      return false;
    }

    try {
      const projectResponse = await firstValueFrom(this.project.setProject(classificationId));

      if (!projectResponse || !projectResponse.successful) {
        const message = projectResponse?.message || 'Failed to load project';
        this.tempData.setTempData('message', { message });
        this.router.navigate([projectResponse?.path || '/projects']);
        return false;
      }

      this.session.setItem(
        this.portalConfig.getClassificationSessionKey(),
        projectResponse.classification,
      );

      const classification = this.session.getItem<Classification>(
        this.portalConfig.getClassificationSessionKey(),
      );
      if (classification != null) {
        this.session.setItem('initialCatalogFromSession', classification?.arumToken);
        this.session.setItem('appNameFromSession', classification?.type);
      } else {
        const allClassifications = this.portalConfig.getAllClassifications();
        if (allClassifications.length !== 1) {
          this.session.removeItem('initialCatalogFromSession');
          this.session.removeItem('appNameFromSession');
        } else {
          this.session.setItem(
            this.portalConfig.getClassificationSessionKey(),
            allClassifications[0],
          );
          this.session.setItem('initialCatalogFromSession', allClassifications[0].arumToken);
        }
      }

      this.project.setApplicationInformation(projectResponse.classification);

      const userToStore = {
        ...projectResponse.arumUser,
        bulletinBoards: [],
      };
      this.session.setItem('arumUser', JSON.stringify(userToStore));

      if (this.portalConfig.enableBB() && projectResponse.arumUser.bulletinBoards?.length !== 0) {
        const newBulletinBoards = projectResponse.arumUser.bulletinBoards.reduce((count, board) => {
          if (board.isNewBulletinBoard) count++;
          if (board.alwaysShow === true) count++;
          return count;
        }, 0);

        if (newBulletinBoards >= 1) {
          this.router.navigate(['/bulletinBoard/showAll']);
          return true;
        }
      }

      this.router.navigate(['/bulletinBoard/showAll']);
      return true;
    } catch (error) {
      console.error('Error in ProjectSelectionGuard:', error);
      this.tempData.setTempData('message', { message: 'Error loading project' });
      this.router.navigate(['/projects']);
      return false;
    }
  }
}
