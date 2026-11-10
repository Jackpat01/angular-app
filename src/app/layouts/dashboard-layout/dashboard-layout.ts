import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { TitleService } from '../../services/title';

@Component({
  imports: [CommonModule, RouterOutlet],
  selector: 'app-dashboard-layout',
  styleUrl: './dashboard-layout.scss',
  templateUrl: './dashboard-layout.html',
})
export class DashboardLayout implements OnInit {
  appVersion = '1.0.0';
  userEmail = 'user@example.com';
  userRole = 'Administrator';
  providerType = 'Provider';
  currentYear = new Date().getFullYear();

  constructor(
    private titleService: TitleService,
    private modalService: NgbModal
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle('Dashboard');
  }

  showHelpModal(): void {
    alert('Please contact Customer Support at support@example.com');
  }

  logout(): void {
    console.log('Logging out...');
    window.location.href = '/';
  }
}

