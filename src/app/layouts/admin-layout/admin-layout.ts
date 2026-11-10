import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { AdminNavigation, NavItem } from '../../shared/admin-navigation/admin-navigation';
import { TitleService } from '../../services/title';

@Component({
  imports: [CommonModule, RouterOutlet, AdminNavigation],
  selector: 'app-admin-layout',
  styleUrl: './admin-layout.scss',
  templateUrl: './admin-layout.html',
})
export class AdminLayout implements OnInit {
  appVersion = '1.0.0';
  userEmail = 'admin@example.com';
  userRole = 'Administrator';
  providerType = 'Admin';
  currentYear = new Date().getFullYear();
  projectName = 'Main Project';

  menuItems: NavItem[] = [
    { label: 'Dashboard', route: '/admin/dashboard' },
    { label: 'Users', route: '/admin/users' },
    { label: 'Reports', route: '/admin/reports', badge: 3 },
    { label: 'Settings', route: '/admin/settings' },
  ];

  constructor(
    private titleService: TitleService,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle('Administration');
  }

  showHelpModal(): void {
    alert('Please contact Customer Support at support@example.com');
  }

  logout(): void {
    console.log('Logging out...');
    window.location.href = '/';
  }
}

