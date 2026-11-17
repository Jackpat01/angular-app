import {Component, OnInit} from '@angular/core';
import {BaseFormComponent} from '../../../shared/base-form.component';

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
  }
