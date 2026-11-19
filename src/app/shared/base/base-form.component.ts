import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { TitleService } from '../../core/services/title';

@Component({
//  selector: 'ohfi-base-form',
  template: '',
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

  protected abstract getPageTitle(): string;
  protected abstract buildForm(): FormGroup;

  get f() {
    return this.form.controls;
  }
}
