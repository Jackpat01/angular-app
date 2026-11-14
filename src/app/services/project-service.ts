import {ApiService} from './api';
import {inject, Injectable} from '@angular/core';
import {ProjectResponse} from '../models/ProjectResponse';
import {Observable, tap} from 'rxjs';
import {Classification} from '../models/Classification';
import {SessionStorageService} from './session-storage-service';

@Injectable({
  providedIn: 'root',
})
export class ProjectService {

  private apiService = inject(ApiService);
  private session = inject(SessionStorageService);


  constructor() {
  }

  getProject(id: string | null): Observable<ProjectResponse> {
    return this.apiService.get<ProjectResponse>(`/project/${id}`);
  }

  setApplicationInformation(classification: Classification) {
    this.apiService.post('/project/setApplicationInfo', {classification}).pipe(
      tap((application) => {
        this.session.setItem('ApplicationInfo', application);
      })
    )

  }
}
