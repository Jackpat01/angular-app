import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { TitleService } from '../services/title';

/**
 * Base component class for form-based components.
 * Provides common form initialization and title management patterns.
 * 
 * Example usage:
 * export class LoginComponent extends BaseFormComponent {
 *   protected getPageTitle() { return 'Login'; }
 *   
 *   protected buildForm(): FormGroup {
 *     return this.fb.group({
 *       email: ['', [Validators.required, Validators.email]],
 *       password: ['', Validators.required]
 *     });
 *   }
 * }
 */
@Component({
  selector: 'app-base-form',
  template: '', // This is an abstract base component
})
export abstract class BaseFormComponent implements OnInit {
  protected fb = inject(FormBuilder);
  protected titleService = inject(TitleService);

  form!: FormGroup;
  submitted = false;

  ngOnInit(): void {
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
   * Convenience getter for form controls
   */
  get f() {
    return this.form.controls;
  }
}
