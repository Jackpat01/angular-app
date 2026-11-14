import {Component, OnInit, inject} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {SessionStorageService} from '../../../services/session-storage-service';
import {ARUMProxy_User} from '../../../models/ARUMProxy_User';
import {Projects} from '../projects';
import {ProjectService} from '../../../services/project-service';
import {TempDataService} from '../../../services/temp-data';
import {PortalConfigService} from '../../../services/portal-config';
import {Classification} from '../../../models/Classification';

@Component({
  selector: 'app-set-project',
  templateUrl: './set-project.html',
  styleUrls: ['./set-project.css']
})
export class SetProjectComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private session = inject(SessionStorageService);
  private project = inject(ProjectService);
  private router = inject(Router);
  private tempData = inject(TempDataService);
  private portalConfig = inject(PortalConfigService);


  ngOnInit(): void {
    this.route.queryParamMap.subscribe(params => {
      let classificationId = params.get('cID');

      let myUser = this.session.getItem<ARUMProxy_User>('arumUser');

      if (myUser == null) {
        this.router.navigate(['/login']);
      }

      this.project.getProject(classificationId).subscribe(project => {
        if (!project.successful) {
          let message = project.message;
          this.tempData.setTempData('message', {message});
          this.router.navigate([project.path]);
        }

        this.session.setItem(this.portalConfig.getClassificationSessionKey(), project.classification);

        let classification = this.session.getItem<Classification>(this.portalConfig.getClassificationSessionKey());
        if (classification != null) {
          this.session.setItem('InitialCatalogFromSession', classification?.arumToken);
          this.session.setItem('AppNameFromSession', classification?.type);
        } else {
          let allClassifications = this.portalConfig.getAllClassifications();
          if (allClassifications.length != 1) {
            this.session.setItem('InitialCatalogFromSession', null);
            this.session.setItem('AppNameFromSession', null);
          } else {
            this.session.setItem(this.portalConfig.getClassificationSessionKey(), allClassifications[0]);
            this.session.setItem('InitialCatalogFromSession', allClassifications[0].arumToken);
          }
        }
        this.project.setApplicationInformation(project.classification);

        // Update ARUM User
        this.session.setItem('arumUser', project.arumUser);

        if (this.portalConfig.enableBB() && (project.arumUser.bulletinBoards?.length!=0)) {
          const newBulletinBoards = project.arumUser.bulletinBoards.reduce((count, board) => {
            if (board.isNewBulletinBoard) count++;
            if (board.alwaysShow === true) count++;
            return count;
          }, 0);

          if (newBulletinBoards >= 1) {
            this.router.navigate(['/bulletinBoard/showAll']);
          }
        }
        this.router.navigate(['/secure']);
      })
    });
  }
}
