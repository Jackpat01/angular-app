import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class TitleService {
  private titleSubject = new BehaviorSubject<string>('Angular App');
  public title$ = this.titleSubject.asObservable();

  constructor(private http: HttpClient) {}

  setTitle(title: string): void {
    this.titleSubject.next(title);
  }

  fetchTitleFromApi(endpoint: string): Observable<string> {
    return this.http.get<{ title: string }>(endpoint).pipe(
      map(response => response.title)
    );
  }
}
