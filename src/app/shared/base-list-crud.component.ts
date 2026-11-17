import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { TitleService } from '../services/title';

/**
 * Base component class for list/CRUD operations.
 * Provides common patterns for displaying, filtering, and managing lists of items.
 * 
 * Type parameter T should have at least an 'id' property.
 * 
 * Example usage:
 * interface User {
 *   id: number;
 *   email: string;
 *   name: string;
 * }
 * 
 * export class UsersComponent extends BaseListCrudComponent<User> {
 *   protected getPageTitle() { return 'Users'; }
 *   
 *   protected buildForm(): FormGroup {
 *     return this.fb.group({ email: [''], name: [''] });
 *   }
 *   
 *   protected loadItems(): void {
 *     this.items = [...];
 *     this.applyFilter();
 *   }
 *   
 *   protected matchesSearchTerm(item: User): boolean {
 *     const term = this.searchTerm.toLowerCase();
 *     return item.email.toLowerCase().includes(term) ||
 *            item.name.toLowerCase().includes(term);
 *   }
 *   
 *   protected onItemDeleted(id: number): void {
 *     // Handle post-delete logic (API call, notifications, etc)
 *   }
 * }
 */
@Component({
  selector: 'ohfi-base-list-crud',
  template: '', // This is an abstract base component
})
export abstract class BaseListCrudComponent<T extends { id: number | string }> {
  protected fb = inject(FormBuilder);
  protected titleService = inject(TitleService);

  items: T[] = [];
  filteredItems: T[] = [];
  searchTerm = '';
  showForm = false;
  form!: FormGroup;
  submitted = false;

  constructor() {
    this.titleService.setTitle(this.getPageTitle());
    this.form = this.buildForm();
  }

  /**
   * Override this method to provide the page title
   */
  protected abstract getPageTitle(): string;

  /**
   * Override this method to build and return the form group
   */
  protected abstract buildForm(): FormGroup;

  /**
   * Override this method to load items (from API, local data, etc)
   */
  protected abstract loadItems(): void;

  /**
   * Override this method to define how items match the search term
   * @param item The item to test
   * @returns true if item matches the current search term
   */
  protected abstract matchesSearchTerm(item: T): boolean;

  /**
   * Override this method to handle post-delete logic
   * @param id The ID of the deleted item
   */
  protected abstract onItemDeleted(id: number | string): void;

  /**
   * Apply the current search filter to items
   */
  applyFilter(): void {
    if (!this.searchTerm.trim()) {
      this.filteredItems = this.items;
    } else {
      this.filteredItems = this.items.filter(item => this.matchesSearchTerm(item));
    }
  }

  /**
   * Toggle the add/edit form visibility
   */
  toggleForm(): void {
    this.showForm = !this.showForm;
    if (!this.showForm) {
      this.form.reset();
      this.submitted = false;
    }
  }

  /**
   * Delete an item with confirmation
   * @param id The ID of the item to delete
   */
  delete(id: number | string): void {
    if (this.confirmDelete()) {
      this.items = this.items.filter(item => item.id !== id);
      this.applyFilter();
      this.onItemDeleted(id);
    }
  }

  /**
   * Show a delete confirmation dialog
   * @returns true if user confirms deletion
   */
  protected confirmDelete(): boolean {
    return confirm('Are you sure you want to delete this item?');
  }

  /**
   * Convenience getter for form controls
   */
  get f() {
    return this.form.controls;
  }
}
