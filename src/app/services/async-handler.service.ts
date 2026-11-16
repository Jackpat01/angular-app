import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { WritableSignal } from '@angular/core';

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
   * @param onError Optional callback when the Observable errors
   */
  executeAsync<T>(
    observable: Observable<T>,
    isLoadingSignal: WritableSignal<boolean>,
    onSuccess: (data: T) => void,
    onError?: (error: any) => void
  ): void {
    isLoadingSignal.set(true);
    observable.subscribe({
      next: (data) => {
        isLoadingSignal.set(false);
        onSuccess(data);
      },
      error: (error) => {
        isLoadingSignal.set(false);
        onError?.(error);
      },
    });
  }

  /**
   * Execute an async operation and return the subscription for manual cleanup
   * @param observable The Observable to subscribe to
   * @param isLoadingSignal Signal to track loading state
   * @param onSuccess Callback when the Observable emits successfully
   * @param onError Optional callback when the Observable errors
   * @returns The subscription (useful for OnDestroy cleanup)
   */
  executeAsyncWithSubscription<T>(
    observable: Observable<T>,
    isLoadingSignal: WritableSignal<boolean>,
    onSuccess: (data: T) => void,
    onError?: (error: any) => void
  ) {
    isLoadingSignal.set(true);
    return observable.subscribe({
      next: (data) => {
        isLoadingSignal.set(false);
        onSuccess(data);
      },
      error: (error) => {
        isLoadingSignal.set(false);
        onError?.(error);
      },
    });
  }
}
