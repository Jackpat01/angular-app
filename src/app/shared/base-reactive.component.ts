import { Component, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';

/**
 * Base component class for reactive components that subscribe to Observables.
 * Provides automatic subscription cleanup via takeUntil pattern.
 *
 * Features:
 * - Automatically unsubscribes from all Observables on component destroy
 * - Prevents memory leaks from forgotten unsubscribe calls
 * - Use destroy$ in pipe: observable$.pipe(takeUntil(this.destroy$)).subscribe()
 *
 * Example usage:
 * export class MyComponent extends BaseReactiveComponent implements OnInit {
 *   data$ = this.dataService.getData();
 *
 *   ngOnInit() {
 *     this.data$.pipe(takeUntil(this.destroy$)).subscribe(data => {
 *       this.handleData(data);
 *     });
 *   }
 * }
 */
@Component({
//  selector: 'ohfi-base-reactive',
  template: '', // This is an abstract base component
})
export abstract class BaseReactiveComponent implements OnDestroy {
  /**
   * Subject used to signal when the component is destroyed.
   * Use this in takeUntil() to automatically unsubscribe from Observables.
   *
   * Example:
   * observable$.pipe(takeUntil(this.destroy$)).subscribe(...)
   */
  protected destroy$ = new Subject<void>();

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
