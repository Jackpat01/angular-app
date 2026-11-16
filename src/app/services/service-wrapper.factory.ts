import { Injectable, inject, Type } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, map } from 'rxjs';

/**
 * Service wrapping utility factory
 * Provides common patterns for creating services that wrap API calls
 * Consolidates loading state, error handling, and response transformation
 */
@Injectable({
  providedIn: 'root'
})
export class ServiceWrapperFactory {
  private httpClient = inject(HttpClient);

  /**
   * Create a wrapper for API GET requests with loading state
   * @param endpoint The API endpoint
   * @param defaultValue The default value if request fails
   * @returns Observable that manages loading state automatically
   */
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

    return {
      request$,
      isLoading$: isLoadingSubject.asObservable(),
    };
  }

  /**
   * Create a wrapper for API POST requests with loading state
   * @param endpoint The API endpoint
   * @param body The request body
   * @returns Observable that manages loading state automatically
   */
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

    return {
      request$,
      isLoading$: isLoadingSubject.asObservable(),
    };
  }

  /**
   * Transform response data with optional mapping
   * @param response The API response
   * @param mapper Optional transformation function
   * @returns Transformed response
   */
  mapResponse<T, U>(response: T, mapper?: (data: T) => U): U | T {
    if (mapper) {
      return mapper(response);
    }
    return response;
  }
}
