import { inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { TestHelpDialogComponent } from './test-help-dialog.component';

export class DialogExampleComponent {
  private dialog = inject(MatDialog);

  openHelpDialog(): void {
    this.dialog.open(TestHelpDialogComponent, {
      width: '500px',
      disableClose: false,
    });
  }
}
