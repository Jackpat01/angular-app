import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { TitleService } from '../../core/services/title';

@Component({
//  selector: 'ohfi-base-list-crud',
  template: '',
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

  protected abstract getPageTitle(): string;
  protected abstract buildForm(): FormGroup;
  protected abstract loadItems(): void;
  protected abstract matchesSearchTerm(item: T): boolean;
  protected abstract onItemDeleted(id: number | string): void;

  applyFilter(): void {
    if (!this.searchTerm.trim()) {
      this.filteredItems = this.items;
    } else {
      this.filteredItems = this.items.filter(item => this.matchesSearchTerm(item));
    }
  }

  toggleForm(): void {
    this.showForm = !this.showForm;
    if (!this.showForm) {
      this.form.reset();
      this.submitted = false;
    }
  }

  delete(id: number | string): void {
    if (this.confirmDelete()) {
      this.items = this.items.filter(item => item.id !== id);
      this.applyFilter();
      this.onItemDeleted(id);
    }
  }

  protected confirmDelete(): boolean {
    return confirm('Are you sure you want to delete this item?');
  }

  get f() {
    return this.form.controls;
  }
}
