import {ApiService} from './api';
import {inject, Injectable} from '@angular/core';
import {ProjectResponse} from '../models/ProjectResponse';
import {BehaviorSubject, Observable} from 'rxjs';
import {finalize, tap} from 'rxjs/operators';
import {Classification} from '../models/Classification';
import {SessionStorageService} from './session-storage-service';
import {SetProjectRequest} from '../models/SetProjectRequest';

@Injectable({
  providedIn: 'root',
})
export class ProjectService {
  private apiService = inject(ApiService);
  private session = inject(SessionStorageService);
  private isLoadingSubject = new BehaviorSubject<boolean>(false);
  isLoading$ = this.isLoadingSubject.asObservable();

  setProject(request: SetProjectRequest): Observable<ProjectResponse> {
    this.isLoadingSubject.next(true);
    return this.apiService
      .post<ProjectResponse>('/project/set-Project', request as unknown as Record<string, unknown>)
      .pipe(finalize(() => this.isLoadingSubject.next(false)));
  }

  setApplicationInformation(classification: Classification): void {
    this.apiService
      .post('/project/retrieveApplicationInfo', {classification})
      .pipe(
        tap((application) => {
          if (application) {
            this.session.setItem('applicationInfo', application);
          }
        }),
      )
      .subscribe({
        error: (error) => console.error('Failed to retrieve application info:', error),
      });
  }
}
