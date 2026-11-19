import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ServiceWrapperFactory {
  private httpClient = inject(HttpClient);

  createGetRequest<T>(
    endpoint: string,
    defaultValue: T
  ): { request$: Observable<T>; isLoading$: Observable<boolean> } {
    const isLoadingSubject = new BehaviorSubject<boolean>(false);

    const request$ = new Observable<T>(observer => {
      isLoadingSubject.next(true);
      this.httpClient.get<T>(endpoint).subscribe({
        next: (data) => {
          isLoadingSubject.next(false);
          observer.next(data);
          observer.complete();
        },
        error: (error) => {
          isLoadingSubject.next(false);
          console.error(`Failed to fetch from ${endpoint}:`, error);
          observer.next(defaultValue);
          observer.complete();
        },
      });
    });

    return { request$, isLoading$: isLoadingSubject.asObservable() };
  }

  createPostRequest<T>(
    endpoint: string,
    body: Record<string, unknown>
  ): { request$: Observable<T>; isLoading$: Observable<boolean> } {
    const isLoadingSubject = new BehaviorSubject<boolean>(false);

    const request$ = new Observable<T>(observer => {
      isLoadingSubject.next(true);
      this.httpClient.post<T>(endpoint, body).subscribe({
        next: (data) => {
          isLoadingSubject.next(false);
          observer.next(data);
          observer.complete();
        },
        error: (error) => {
          isLoadingSubject.next(false);
          console.error(`Failed to POST to ${endpoint}:`, error);
          observer.error(error);
        },
      });
    });

    return { request$, isLoading$: isLoadingSubject.asObservable() };
  }

  mapResponse<T, U>(response: T, mapper?: (data: T) => U): U | T {
    if (mapper) {
      return mapper(response);
    }
    return response;
  }
}
