import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  width?: string;
  align?: 'left' | 'center' | 'right';
  format?: (value: unknown) => string;
}

export interface TableAction {
  label: string;
  icon?: string;
  class?: string;
  disabled?: (item: unknown) => boolean;
  handler: (id: number | string) => void;
}

/**
 * Reusable table component for displaying and managing lists of items.
 * Supports custom columns, actions, and empty states.
 * 
 * Example usage:
 * <app-data-table
 *   [items]="users"
 *   [columns]="userColumns"
 *   [actions]="userActions"
 *   emptyMessage="No users found">
 * </app-data-table>
 */
@Component({
  selector: 'ohfi-data-table',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card">
      <div class="card-header bg-light">
        <h5 class="mb-0">{{ title }} ({{ items.length }} found)</h5>
      </div>
      <div class="card-body">
        <div class="table-responsive">
          <table class="table table-hover">
            <thead class="table-light">
              <tr>
                @for (column of columns; track column.key) {
                  <th [style.width]="column.width" [style.text-align]="column.align || 'left'">
                    {{ column.label }}
                  </th>
                }
                @if (actions && actions.length > 0) {
                  <th>Actions</th>
                }
              </tr>
            </thead>
            <tbody>
              @for (item of items; track getItemId(item)) {
                <tr>
                  @for (column of columns; track column.key) {
                    <td [style.text-align]="column.align || 'left'">
                      @if (column.format) {
                        {{ column.format(getNestedProperty(item, column.key)) }}
                      } @else {
                        {{ getNestedProperty(item, column.key) }}
                      }
                    </td>
                  }
                  @if (actions && actions.length > 0) {
                    <td>
                      @for (action of actions; track action.label) {
                        <button
                          (click)="action.handler(getItemId(item))"
                          [disabled]="action.disabled && action.disabled(item)"
                          [class]="action.class || 'btn btn-sm btn-secondary'"
                          class="me-2">
                          @if (action.icon) {
                            <i [class]="action.icon"></i>
                          }
                          {{ action.label }}
                        </button>
                      }
                    </td>
                  }
                </tr>
              }
              @if (items.length === 0) {
                <tr>
                  <td
                    class="text-center text-muted py-4"
                    [attr.colspan]="columns.length + (actions && actions.length > 0 ? 1 : 0)">
                    {{ emptyMessage }}
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `,
})
export class DataTableComponent {
  @Input() items: unknown[] = [];
  @Input() columns: TableColumn[] = [];
  @Input() actions?: TableAction[];
  @Input() title = 'Items';
  @Input() emptyMessage = 'No items found';

  @Output() actionTriggered = new EventEmitter<{ action: string; id: number | string }>();

  getItemId(item: unknown): number | string {
    return (item as Record<string, unknown>)['id'] as number | string;
  }

  getNestedProperty(obj: unknown, path: string): unknown {
    return path.split('.').reduce((current, prop) => 
      (current as Record<string, unknown>)?.[prop], obj);
  }
}
