import { Injectable, inject } from "@angular/core";
import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { Observable, throwError } from "rxjs";
import { catchError } from "rxjs/operators";

@Injectable({
  providedIn: "root",
})
export class ApiService {
  private readonly http = inject(HttpClient);
  private apiBaseUrl = "http://localhost:5239/api";

  /**
   * GET request
   */
  get<T>(endpoint: string): Observable<T> {
    return this.http.get<T>(`${this.apiBaseUrl}${endpoint}`).pipe(
      catchError((error: HttpErrorResponse) => this.handleError(error))
    );
  }

  getRemoteData(endpoint: string): Observable<string> {
    return this.http
      .get(endpoint, { responseType: "text" })
      .pipe(catchError((error: HttpErrorResponse) => this.handleError(error)));
  }

  /**
   * POST request
   */
  post<T>(endpoint: string, body: Record<string, unknown>): Observable<T> {
    return this.http.post<T>(`${this.apiBaseUrl}${endpoint}`, body).pipe(
      catchError((error: HttpErrorResponse) => this.handleError(error))
    );
  }

  /**
   * PUT request
   */
  put<T>(endpoint: string, body: Record<string, unknown>): Observable<T> {
    return this.http.put<T>(`${this.apiBaseUrl}${endpoint}`, body).pipe(
      catchError((error: HttpErrorResponse) => this.handleError(error))
    );
  }

  /**
   * PATCH request
   */
  patch<T>(endpoint: string, body: Record<string, unknown>): Observable<T> {
    return this.http.patch<T>(`${this.apiBaseUrl}${endpoint}`, body).pipe(
      catchError((error: HttpErrorResponse) => this.handleError(error))
    );
  }

  /**
   * DELETE request
   */
  delete<T = void>(endpoint: string): Observable<T> {
    return this.http.delete<T>(`${this.apiBaseUrl}${endpoint}`).pipe(
      catchError((error: HttpErrorResponse) => this.handleError(error))
    );
  }

  /**
   * Set the API base URL
   */
  setApiBaseUrl(url: string): void {
    this.apiBaseUrl = url;
  }

  /**
   * Get the current API base URL
   */
  getApiBaseUrl(): string {
    return this.apiBaseUrl;
  }

  /**
   * Handle HTTP errors
   */
  private handleError(error: HttpErrorResponse) {
    let errorMessage = "An error occurred";

    if (error.error instanceof ErrorEvent) {
      errorMessage = `Error: ${error.error.message}`;
    } else {
      errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
      if (error.error && typeof error.error === "object") {
        const errorObj = error.error as Record<string, unknown>;
        if (errorObj.message) {
          errorMessage = String(errorObj.message);
        }
      }
    }

    console.error(errorMessage);
    return throwError(() => new Error(errorMessage));
  }
}
