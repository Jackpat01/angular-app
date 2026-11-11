import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  imports: [CommonModule],
  selector: 'ohfi-logo-branding',
  styleUrl: './logo-branding.css',
  templateUrl: './logo-branding.html',
})
export class LogoBrandingComponent {
  @Input() showUserInfo = false;
  @Input() userEmail = '';
  @Input() userRole = '';
  @Input() providerType = '';
  @Input() projectName = '';
}
