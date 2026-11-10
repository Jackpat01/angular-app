import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  imports: [CommonModule],
  selector: 'app-logo-branding',
  styleUrl: './logo-branding.scss',
  templateUrl: './logo-branding.html',
})
export class LogoBrandingComponent {
  @Input() showUserInfo = false;
  @Input() userEmail = '';
  @Input() userRole = '';
  @Input() providerType = '';
  @Input() projectName = '';
}
