import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Reusable error display component for forms and pages.
 * Shows error messages with consistent styling.
 *
 * Example usage:
 * <app-error-display
 *   [message]="errorMessage"
 *   [visible]="showError"
 *   (dismiss)="onDismissError()">
 * </app-error-display>
 */
@Component({
//  selector: 'ohfi-error-display',
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (message && visible) {
      <div class="alert alert-danger alert-dismissible fade show" role="alert">
        <div class="d-flex align-items-start gap-2">
          <div>
            <strong>Error:</strong> {{ message }}
          </div>
          <button
            (click)="onDismiss()"
            aria-label="Close"
            class="btn-close"
            type="button">
          </button>
        </div>
      </div>
    }
  `,
  styles: [`
    .alert {
      margin-bottom: 1rem;
    }

    .btn-close {
      flex-shrink: 0;
      margin-top: 0.25rem;
    }
  `]
})
export class ErrorDisplayComponent {
  @Input() message: string | null = null;
  @Input() visible = false;
  @Output() dismiss = new EventEmitter<void>();

  onDismiss(): void {
    this.dismiss.emit();
  }
}
