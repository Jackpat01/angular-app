import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { BaseReactiveComponent } from '../../../shared/base-reactive.component';

@Component({
  selector: 'ohfi-set-project',
  templateUrl: './set-project.html',
  styleUrls: ['./set-project.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SetProjectComponent extends BaseReactiveComponent implements OnInit {
  private router = inject(Router);

  ngOnInit(): void {
    // All data processing is done in ProjectSelectionGuard
    // Navigate to the secure page
    this.router.navigate(['/secure']);
  }
}
