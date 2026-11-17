import { Component, OnInit, inject, ChangeDetectionStrategy, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SessionStorageService } from '../../../services/session-storage-service';
import { ARUMProxy_User } from '../../../models/ARUMProxy_User';
import { TempDataService } from '../../../services/temp-data';
import { PortalConfigService } from '../../../services/portal-config';
import { Classification } from '../../../models/Classification';
import { AsyncHandlerService } from '../../../services/async-handler.service';
import { BaseReactiveComponent } from '../../../shared/base-reactive.component';
import { ProjectService } from '../../../services/project-service';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'ohfi-set-project',
  templateUrl: './set-project.html',
  styleUrls: ['./set-project.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SetProjectComponent extends BaseReactiveComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private session = inject(SessionStorageService);
  private project = inject(ProjectService);
  private router = inject(Router);
  private tempData = inject(TempDataService);
  private portalConfig = inject(PortalConfigService);
  private asyncHandler = inject(AsyncHandlerService);
  isLoading = signal(false);

  ngOnInit(): void {
    this.portalConfig
      .waitForConfigLoad()
      .then(() => {
        this.route.queryParamMap.pipe(takeUntil(this.destroy$)).subscribe((params) => {
          const classificationId = params.get('cID');
          const myUser = this.session.getItem<ARUMProxy_User>('arumUser');

          if (myUser == null) {
            this.router.navigate(['/login']);
            return;
          }

          this.asyncHandler.executeAsync(
            this.project.getProject(classificationId),
            this.isLoading,
            (project) => {
              if (!project.successful) {
                const message = project.message;
                this.tempData.setTempData('message', { message });
                this.router.navigate([project.path]);
                return;
              }

              this.session.setItem(
                this.portalConfig.getClassificationSessionKey(),
                project.classification,
              );

              const classification = this.session.getItem<Classification>(
                this.portalConfig.getClassificationSessionKey(),
              );
              if (classification != null) {
                this.session.setItem('InitialCatalogFromSession', classification?.arumToken);
                this.session.setItem('AppNameFromSession', classification?.type);
              } else {
                const allClassifications = this.portalConfig.getAllClassifications();
                if (allClassifications.length != 1) {
                  this.session.setItem('InitialCatalogFromSession', null);
                  this.session.setItem('AppNameFromSession', null);
                } else {
                  this.session.setItem(
                    this.portalConfig.getClassificationSessionKey(),
                    allClassifications[0],
                  );
                  this.session.setItem(
                    'InitialCatalogFromSession',
                    allClassifications[0].arumToken,
                  );
                }
              }
              this.project.setApplicationInformation(project.classification);

              this.session.setItem('arumUser', project.arumUser);

              if (this.portalConfig.enableBB() && project.arumUser.bulletinBoards?.length != 0) {
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
            },
            (errorMessage: string) => {
              console.error('Error loading project:', errorMessage);
              this.router.navigate(['/login']);
            },
          );
        });
      })
      .catch((error) => {
        console.error('Error loading portal config:', error);
        this.router.navigate(['/login']);
      });
  }
}
