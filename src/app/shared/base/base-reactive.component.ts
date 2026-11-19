import { Component, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';

@Component({
//  selector: 'ohfi-base-reactive',
  template: '',
})
export abstract class BaseReactiveComponent implements OnDestroy {
  protected destroy$ = new Subject<void>();

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
