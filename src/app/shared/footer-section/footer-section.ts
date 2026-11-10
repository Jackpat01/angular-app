import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  imports: [CommonModule],
  selector: 'ohfi-footer-section',
  styleUrl: './footer-section.scss',
  templateUrl: './footer-section.html',
})
export class FooterSectionComponent {
  @Input() appVersion = '1.0.0';
  @Input() currentYear = new Date().getFullYear();
}
