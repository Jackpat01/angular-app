import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  selector: 'ohfi-help-modal',
  styleUrl: './help-modal.css',
  templateUrl: './help-modal.html',
})
export class HelpModalComponent {
  constructor(@Inject(MAT_DIALOG_DATA) public data: { helpDeskEmail: string }) {}

}
