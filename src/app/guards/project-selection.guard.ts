import {inject} from '@angular/core';
import {ActivatedRouteSnapshot, CanActivateFn, Router} from '@angular/router';
import {firstValueFrom} from 'rxjs';
import {SessionStorageService} from '../services/session-storage-service';
import {ProjectService} from '../services/project-service';
import {PortalConfigService} from '../services/portal-config';
import {TempDataService} from '../services/temp-data';
import {ARUMProxy_User} from '../models/ARUMProxy_User';
import {Classification} from '../models/Classification';
import {SetProjectRequest} from '../models/SetProjectRequest';

export const projectSelectionGuard: CanActivateFn = async (route: ActivatedRouteSnapshot) => {
  const session = inject(SessionStorageService);
  const project = inject(ProjectService);
  const portalConfig = inject(PortalConfigService);
  const tempData = inject(TempDataService);
  const router = inject(Router);

  const classificationId = route.queryParams['cID'];
  const myUser = session.getItem<ARUMProxy_User>('arumUser');

  if (!myUser) {
    return router.createUrlTree(['/login']);
  }

  if (!classificationId) {
    return router.createUrlTree(['/projects']);
  }

  try {
    const isSSOUser = session.getItem<boolean>('IsSSO') || false;
    const setProjectRequest: SetProjectRequest = {
      id: classificationId,
      email: myUser.email!,
      isSSOUser,
    };

    const projectResponse = await firstValueFrom(project.setProject(setProjectRequest));

    if (!projectResponse || !projectResponse.successful) {
      const message = projectResponse?.message || 'Failed to load project';
      tempData.setTempData('message', {message});
      return router.createUrlTree([projectResponse?.path || '/projects']);
    }

    session.setItem(portalConfig.getClassificationSessionKey(), projectResponse.classification);

    const classification = session.getItem<Classification>(
      portalConfig.getClassificationSessionKey(),
    );
    if (classification != null) {
      session.setItem('initialCatalogFromSession', classification?.arumToken);
      session.setItem('appNameFromSession', classification?.type);
    } else {
      const allClassifications = portalConfig.getAllClassifications();
      if (allClassifications.length !== 1) {
        session.removeItem('initialCatalogFromSession');
        session.removeItem('appNameFromSession');
      } else {
        session.setItem(portalConfig.getClassificationSessionKey(), allClassifications[0]);
        session.setItem('initialCatalogFromSession', allClassifications[0].arumToken);
      }
    }

    project.setApplicationInformation(projectResponse.classification);

    const userToStore = {...projectResponse.arumUser, bulletinBoards: []};
    session.setItem('arumUser', JSON.stringify(userToStore));

    if (portalConfig.enableBB() && projectResponse.arumUser.bulletinBoards?.length !== 0) {
      const newBulletinBoards = projectResponse.arumUser.bulletinBoards.reduce((count, board) => {
        if (board.isNewBulletinBoard) count++;
        if (board.alwaysShow === true) count++;
        return count;
      }, 0);

      if (newBulletinBoards >= 1) {
        return router.createUrlTree(['/bulletinBoard/showAll']);
      }
    }

    return router.createUrlTree(['/bulletinBoard/showAll']);
  } catch (error) {
    console.error('Error in projectSelectionGuard:', error);
    tempData.setTempData('message', {message: 'Error loading project'});
    return router.createUrlTree(['/projects']);
  }
};
