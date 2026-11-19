import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';

export interface HelpDialogData {
  email: string;
}

@Component({
//  selector: 'ohfi-help-modal',
  imports: [CommonModule, MatDialogModule],
  templateUrl: './help-modal.html',
  styleUrls: ['./help-modal.css'],
})
export class HelpDialogComponent {
  private dialogRef = inject(MatDialogRef<HelpDialogComponent>);
  data = inject(MAT_DIALOG_DATA) as HelpDialogData;

  onClose(): void {
    this.dialogRef.close();
  }
}
