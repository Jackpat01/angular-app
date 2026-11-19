import {Component, DestroyRef, inject} from '@angular/core';

/**
 * Base component for reactive components requiring Observable cleanup.
 * Provides Angular's DestroyRef for use with takeUntilDestroyed().
 *
 * Example usage:
 * this.data$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(...)
 */
@Component({
  template: '',
})
export abstract class BaseReactiveComponent {
  protected readonly destroyRef = inject(DestroyRef);
}
