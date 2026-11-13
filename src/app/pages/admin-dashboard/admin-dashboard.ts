import {Component, OnInit, inject} from '@angular/core';
import {CommonModule} from '@angular/common';
import {TitleService} from '../../services/title';

interface Stat {
  label: string;
  value: number | string;
  icon: string;
}

interface Activity {
  action: string;
  user: string;
  timestamp: string;
}

@Component({
  imports: [CommonModule],
  selector: 'ohfi-admin-dashboard',
  styleUrl: './admin-dashboard.css',
  templateUrl: './admin-dashboard.html',
})
export class AdminDashboard implements OnInit {
  private titleService = inject(TitleService);
  stats: Stat[] = [
    {label: 'Total Users', value: 1234, icon: 'users'},
    {label: 'Active Sessions', value: 45, icon: 'activity'},
    {label: 'Pending Reports', value: 12, icon: 'alert'},
    {label: 'System Health', value: '98%', icon: 'trending-up'},
  ];

  recentActivity: Activity[] = [
    {action: 'User login', user: 'john.doe@example.com', timestamp: 'Today 10:30 AM'},
    {action: 'Report generated', user: 'jane.smith@example.com', timestamp: 'Today 09:15 AM'},
    {action: 'Settings updated', user: 'admin@example.com', timestamp: 'Yesterday 02:45 PM'},
    {action: 'User registered', user: 'new.user@example.com', timestamp: 'Yesterday 11:20 AM'},
  ];

  ngOnInit(): void {
    this.titleService.setTitle('Admin Dashboard');
  }
}
