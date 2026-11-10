import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { Title } from '@angular/platform-browser';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class TitleService {
  private titleSubject = new BehaviorSubject<string>('Angular App');
  public title$ = this.titleSubject.asObservable();

  constructor(
    private http: HttpClient,
    private browserTitle: Title
  ) {}

  setTitle(title: string): void {
    this.titleSubject.next(title);
    // Update browser tab title
    this.browserTitle.setTitle(`${title} | OHFI`);
  }

  fetchTitleFromApi(endpoint: string): Observable<string> {
    return this.http.get<{ title: string }>(endpoint).pipe(
      map(response => response.title)
    );
  }
}

