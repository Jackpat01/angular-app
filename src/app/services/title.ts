import {Injectable, inject} from '@angular/core';
import {BehaviorSubject, Observable} from 'rxjs';
import {HttpClient} from '@angular/common/http';
import {Title} from '@angular/platform-browser';
import {map} from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class TitleService {
  private http = inject(HttpClient);
  private browserTitle = inject(Title);
  private titleSubject = new BehaviorSubject<string>('Angular App');
  public title$ = this.titleSubject.asObservable();

  setTitle(title: string): void {
    this.titleSubject.next(title);
    this.browserTitle.setTitle(`${title}`);
  }

  fetchTitleFromApi(endpoint: string): Observable<string> {
    return this.http.get<{ title: string }>(endpoint).pipe(
      map(response => response.title)
    );
  }
}
