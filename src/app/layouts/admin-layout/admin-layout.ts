import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HeaderBarComponent } from '../../shared/header-bar/header-bar';
import { LogoBrandingComponent } from '../../shared/logo-branding/logo-branding';
import { FooterSectionComponent } from '../../shared/footer-section/footer-section';
import { HelpModalComponent } from '../../shared/help-modal/help-modal';
import { LoadingOverlayComponent } from '../../shared/loading-overlay/loading-overlay';
import { AdminNavigation, NavItem } from '../../shared/admin-navigation/admin-navigation';
import { TitleService } from '../../services/title';

@Component({
  imports: [
    CommonModule,
    RouterOutlet,
    HeaderBarComponent,
    LogoBrandingComponent,
    FooterSectionComponent,
    HelpModalComponent,
    LoadingOverlayComponent,
    AdminNavigation,
  ],
  selector: 'app-admin-layout',
  styleUrl: './admin-layout.scss',
  templateUrl: './admin-layout.html',
})
export class AdminLayout implements OnInit {
  appVersion = '1.0.0';
  currentYear = new Date().getFullYear();
  userEmail = 'admin@example.com';
  userRole = 'Administrator';
  providerType = 'Admin';
  projectName = 'Main Project';
  isLoading = false;

  menuItems: NavItem[] = [
    { label: 'Dashboard', route: '/admin/dashboard' },
    { label: 'Users', route: '/admin/users' },
    { label: 'Reports', route: '/admin/reports', badge: 3 },
    { label: 'Settings', route: '/admin/settings' },
  ];

  constructor(private titleService: TitleService) {}

  ngOnInit(): void {
    this.titleService.setTitle('Administration');
  }

  logout(): void {
    console.log('Logging out...');
    window.location.href = '/';
  }
}

