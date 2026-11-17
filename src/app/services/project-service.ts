import {ApiService} from './api';
import {inject, Injectable} from '@angular/core';
import {ProjectResponse} from '../models/ProjectResponse';
import {BehaviorSubject, Observable, tap} from 'rxjs';
import {Classification} from '../models/Classification';
import {SessionStorageService} from './session-storage-service';
import {SetProjectRequest} from '../models/SetProjectRequest';
import {LoginResponse} from '../models/loginResponse';

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
    return new Observable(observer => {
      this.apiService.post<ProjectResponse>('/project/set-Project', request as unknown as Record<string, unknown>).subscribe({
        next: (response) => {
          this.isLoadingSubject.next(false);
          observer.next(response);
          observer.complete();
        },
        error: (error) => {
          this.isLoadingSubject.next(false);
          observer.error(error);
        }
      });
    });
  }

  setApplicationInformation(classification: Classification) {
    this.apiService.post('/project/retrieveApplicationInfo', {classification}).pipe(
      tap((application) => {
        if (application) {
          this.session.setItem('applicationInfo', application);
        }
      })
    )
  }
}
