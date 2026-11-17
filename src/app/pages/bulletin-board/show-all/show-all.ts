import {Component, OnInit} from '@angular/core';
import {BaseFormComponent} from '../../../shared/base-form.component';
import {FormGroup, Validators} from '@angular/forms';

@Component({
  imports: [],
  selector: 'ohfi-show-all',
  styleUrl: './show-all.scss',
  templateUrl: './show-all.html',
})
export class ShowAll extends BaseFormComponent implements OnInit {
  protected getPageTitle(): string {
    return 'Login';
  }
  override ngOnInit(): void {

  }

  protected buildForm(): FormGroup {
    return this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

}
