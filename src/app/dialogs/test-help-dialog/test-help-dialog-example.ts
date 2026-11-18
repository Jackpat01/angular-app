import { inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { TestHelpDialogComponent, TestHelpDialogData } from './test-help-dialog.component';

export class DialogExampleComponent {
  private dialog = inject(MatDialog);

  openHelpDialog(): void {
    const dialogData: TestHelpDialogData = {
      email: 'tech@abc.net',
    };

    this.dialog.open(TestHelpDialogComponent, {
      width: '500px',
      disableClose: false,
      data: dialogData,
    });
  }
}
