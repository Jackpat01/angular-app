import {Component, Input} from '@angular/core';
import {CommonModule} from '@angular/common';

@Component({
  imports: [CommonModule],
  selector: 'ohfi-help-modal',
  styleUrl: './help-modal.css',
  templateUrl: './help-modal.html',
})
export class HelpModalComponent {
  @Input() supportEmail = 'support@example.com';

  showHelpModal(): void {
    alert(`Please contact Customer Support at ${this.supportEmail}`);
  }
}
