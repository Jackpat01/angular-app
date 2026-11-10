import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeaderBarComponent } from '../../shared/header-bar/header-bar';
import { LogoBrandingComponent } from '../../shared/logo-branding/logo-branding';
import { ContentPanelComponent } from '../../shared/content-panel/content-panel';
import { FooterSectionComponent } from '../../shared/footer-section/footer-section';
import { HelpModalComponent } from '../../shared/help-modal/help-modal';
import { LoadingOverlayComponent } from '../../shared/loading-overlay/loading-overlay';
import { TitleService } from '../../services/title';

@Component({
  imports: [
    CommonModule,
    HeaderBarComponent,
    LogoBrandingComponent,
    ContentPanelComponent,
    FooterSectionComponent,
    HelpModalComponent,
    LoadingOverlayComponent,
  ],
  selector: 'app-dashboard-layout',
  styleUrl: './dashboard-layout.scss',
  templateUrl: './dashboard-layout.html',
})
export class DashboardLayout implements OnInit {
  appVersion = '1.0.0';
  currentYear = new Date().getFullYear();
  userEmail = 'user@example.com';
  userRole = 'Administrator';
  providerType = 'Provider';
  projectName = 'Dashboard';
  isLoading = false;

  constructor(private titleService: TitleService) {}

  ngOnInit(): void {
    this.titleService.setTitle('Dashboard');
  }

  logout(): void {
    console.log('Logging out...');
    window.location.href = '/';
  }
}

