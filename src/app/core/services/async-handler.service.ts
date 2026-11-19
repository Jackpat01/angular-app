import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { WritableSignal } from '@angular/core';
import { getErrorMessage } from './error-utils';

@Injectable({ providedIn: 'root' })
export class AsyncHandlerService {
  executeAsync<T>(
    observable: Observable<T>,
    isLoadingSignal: WritableSignal<boolean>,
    onSuccess: (data: T) => void,
    onError?: (error: string) => void,
  ): void {
    isLoadingSignal.set(true);
    observable.subscribe({
      next: (data) => {
        isLoadingSignal.set(false);
        onSuccess(data);
      },
      error: (error) => {
        isLoadingSignal.set(false);
        const errorMessage = getErrorMessage(error);
        onError?.(errorMessage);
      },
    });
  }

  executeAsyncWithSubscription<T>(
    observable: Observable<T>,
    isLoadingSignal: WritableSignal<boolean>,
    onSuccess: (data: T) => void,
    onError?: (error: string) => void,
  ) {
    isLoadingSignal.set(true);
    return observable.subscribe({
      next: (data) => {
        isLoadingSignal.set(false);
        onSuccess(data);
      },
      error: (error) => {
        isLoadingSignal.set(false);
        const errorMessage = getErrorMessage(error);
        onError?.(errorMessage);
      },
    });
  }
}
