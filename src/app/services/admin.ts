import {Injectable} from '@angular/core';
import {BehaviorSubject, Observable} from 'rxjs';
import {ApiService} from './api';

export interface NavItem {
  label: string;
  route: string;
  icon?: string;
  badge?: number;
}

@Injectable({
  providedIn: 'root',
})
export class AdminService {
  private menuItemsSubject = new BehaviorSubject<NavItem[]>([
    {label: 'Dashboard', route: '/admin/dashboard'},
    {label: 'Users', route: '/admin/users'},
    {label: 'Reports', route: '/admin/reports', badge: 3},
    {label: 'Settings', route: '/admin/settings'},
  ]);

  menuItems$ = this.menuItemsSubject.asObservable();

  constructor(private apiService: ApiService) {
    this.loadMenuItems();
  }

  getMenuItems(): NavItem[] {
    return this.menuItemsSubject.value;
  }

  setMenuItems(items: NavItem[]): void {
    this.menuItemsSubject.next(items);
  }

  updateMenuItemBadge(route: string, badge: number | undefined): void {
    const items = this.menuItemsSubject.value;
    const updated = items.map((item) =>
      item.route === route ? {...item, badge} : item
    );
    this.menuItemsSubject.next(updated);
  }

  /**
   * Get user statistics for admin dashboard
   * Endpoint: GET /api/admin/stats
   */
  getStatistics(): Observable<any> {
    return this.apiService.get('/admin/stats');
  }

  /**
   * Get all users
   * Endpoint: GET /api/admin/users
   */
  getUsers(): Observable<any[]> {
    return this.apiService.get<any[]>('/admin/users');
  }

  /**
   * Get all reports
   * Endpoint: GET /api/admin/reports
   */
  getReports(): Observable<any[]> {
    return this.apiService.get<any[]>('/admin/reports');
  }

  /**
   * Create a report
   * Endpoint: POST /api/admin/reports
   */
  createReport(reportData: any): Observable<any> {
    return this.apiService.post('/admin/reports', reportData);
  }

  /**
   * Update a report
   * Endpoint: PUT /api/admin/reports/:id
   */
  updateReport(id: string, reportData: any): Observable<any> {
    return this.apiService.put(`/admin/reports/${id}`, reportData);
  }

  /**
   * Delete a report
   * Endpoint: DELETE /api/admin/reports/:id
   */
  deleteReport(id: string): Observable<any> {
    return this.apiService.delete(`/admin/reports/${id}`);
  }

  /**
   * Load menu items from API
   * Endpoint: GET /api/admin/menu
   */
  private loadMenuItems(): void {
    this.apiService.get<NavItem[]>('/admin/menu').subscribe({
      next: (items) => {
        this.menuItemsSubject.next(items);
      },
      error: (error) => {
        console.error('Failed to load menu items from API, using defaults', error);
        // Keep using default values on error
      }
    });
  }
}
