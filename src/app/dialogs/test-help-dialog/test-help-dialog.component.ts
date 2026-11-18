import { Component, inject } from '@angular/core';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'ohfi-test-help-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule],
  templateUrl: './test-help-dialog.component.html',
  styleUrls: ['./test-help-dialog.component.css'],
})
export class TestHelpDialogComponent {
  private dialogRef = inject(MatDialogRef<TestHelpDialogComponent>);

  onClose(): void {
    this.dialogRef.close();
  }
}
