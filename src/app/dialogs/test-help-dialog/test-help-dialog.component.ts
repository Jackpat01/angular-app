import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';

export interface TestHelpDialogData {
  email: string;
}

@Component({
  selector: 'ohfi-test-help-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule],
  templateUrl: './test-help-dialog.component.html',
  styleUrls: ['./test-help-dialog.component.css'],
})
export class TestHelpDialogComponent {
  private dialogRef = inject(MatDialogRef<TestHelpDialogComponent>);
  data = inject(MAT_DIALOG_DATA) as TestHelpDialogData;

  onClose(): void {
    this.dialogRef.close();
  }
}
