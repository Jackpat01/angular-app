import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

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
    { label: 'Dashboard', route: '/admin/dashboard' },
    { label: 'Users', route: '/admin/users' },
    { label: 'Reports', route: '/admin/reports', badge: 3 },
    { label: 'Settings', route: '/admin/settings' },
  ]);

  menuItems$ = this.menuItemsSubject.asObservable();

  getMenuItems(): NavItem[] {
    return this.menuItemsSubject.value;
  }

  setMenuItems(items: NavItem[]): void {
    this.menuItemsSubject.next(items);
  }

  updateMenuItemBadge(route: string, badge: number | undefined): void {
    const items = this.menuItemsSubject.value;
    const updated = items.map((item) =>
      item.route === route ? { ...item, badge } : item
    );
    this.menuItemsSubject.next(updated);
  }
}
