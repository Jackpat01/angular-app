import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  imports: [CommonModule],
  selector: 'ohfi-loading-overlay',
  styleUrl: './loading-overlay.css',
  templateUrl: './loading-overlay.html',
})
export class LoadingOverlayComponent {
  @Input() isLoading = false;
}
