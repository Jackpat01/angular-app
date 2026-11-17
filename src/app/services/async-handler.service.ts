import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { WritableSignal } from '@angular/core';
import { getErrorMessage } from './error-utils';

/**
 * Utility service to handle common async patterns.
 * Manages loading state and error handling for Observable subscriptions.
 */
@Injectable({ providedIn: 'root' })
export class AsyncHandlerService {
  /**
   * Execute an async operation with automatic loading state management
   * @param observable The Observable to subscribe to
   * @param isLoadingSignal Signal to track loading state
   * @param onSuccess Callback when the Observable emits successfully
   * @param onError Optional callback when the Observable errors (receives error message string)
   */
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

  /**
   * Execute an async operation and return the subscription for manual cleanup
   * @param observable The Observable to subscribe to
   * @param isLoadingSignal Signal to track loading state
   * @param onSuccess Callback when the Observable emits successfully
   * @param onError Optional callback when the Observable errors (receives error message string)
   * @returns The subscription (useful for OnDestroy cleanup)
   */
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
