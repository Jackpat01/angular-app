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

  post<T>(endpoint: string, body: Record<string, unknown>): Observable<T> {
    return this.http.post<T>(`${this.apiBaseUrl}${endpoint}`, body).pipe(
      catchError((error: HttpErrorResponse) => this.handleError(error))
    );
  }

  put<T>(endpoint: string, body: Record<string, unknown>): Observable<T> {
    return this.http.put<T>(`${this.apiBaseUrl}${endpoint}`, body).pipe(
      catchError((error: HttpErrorResponse) => this.handleError(error))
    );
  }

  patch<T>(endpoint: string, body: Record<string, unknown>): Observable<T> {
    return this.http.patch<T>(`${this.apiBaseUrl}${endpoint}`, body).pipe(
      catchError((error: HttpErrorResponse) => this.handleError(error))
    );
  }

  delete<T = void>(endpoint: string): Observable<T> {
    return this.http.delete<T>(`${this.apiBaseUrl}${endpoint}`).pipe(
      catchError((error: HttpErrorResponse) => this.handleError(error))
    );
  }

  setApiBaseUrl(url: string): void {
    this.apiBaseUrl = url;
  }

  getApiBaseUrl(): string {
    return this.apiBaseUrl;
  }

  private handleError(error: HttpErrorResponse) {
    let errorMessage = "An error occurred";

    if (error.error instanceof ErrorEvent) {
      errorMessage = `Error: ${error.error.message}`;
    } else {
      errorMessage = `Error Code: ${error.status}`;
      if (error.message) {
        errorMessage += `\nMessage: ${error.message}`;
      }
      if (error.error && typeof error.error === "object") {
        const errorObj = error.error as Record<string, unknown>;
        if (errorObj['message']) {
          errorMessage = String(errorObj['message']);
        }
      }
    }

    console.error('API Error:', errorMessage, error);
    return throwError(() => new Error(errorMessage));
  }
}
