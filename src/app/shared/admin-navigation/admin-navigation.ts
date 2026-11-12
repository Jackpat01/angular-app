import {Component, Input} from '@angular/core';
import {CommonModule} from '@angular/common';
import {RouterLink} from '@angular/router';

export interface NavItem {
  label: string;
  route: string;
  icon?: string;
  badge?: number;
}

@Component({
  imports: [CommonModule, RouterLink],
  selector: 'ohfi-admin-navigation',
  styleUrl: './admin-navigation.css',
  templateUrl: './admin-navigation.html',
})
export class AdminNavigation {
  @Input() menuItems: NavItem[] = [
    {label: 'Dashboard', route: '/admin/dashboard'},
    {label: 'Users', route: '/admin/users'},
    {label: 'Reports', route: '/admin/reports'},
    {label: 'Settings', route: '/admin/settings'},
  ];
}
