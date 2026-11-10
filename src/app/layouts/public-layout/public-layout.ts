import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeaderBarComponent } from '../../shared/header-bar/header-bar';
import { LogoBrandingComponent } from '../../shared/logo-branding/logo-branding';
import { ContentPanelComponent } from '../../shared/content-panel/content-panel';
import { FooterSectionComponent } from '../../shared/footer-section/footer-section';
import { TitleService } from '../../services/title';

@Component({
  imports: [CommonModule, HeaderBarComponent, LogoBrandingComponent, ContentPanelComponent, FooterSectionComponent],
  selector: 'app-public-layout',
  styleUrl: './public-layout.scss',
  templateUrl: './public-layout.html',
})
export class PublicLayout implements OnInit {
  appVersion = '1.0.0';
  currentYear = new Date().getFullYear();

  constructor(private titleService: TitleService) {}

  ngOnInit(): void {
    this.titleService.setTitle('Welcome');
  }
}

