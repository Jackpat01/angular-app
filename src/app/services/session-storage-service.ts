import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Injectable({
  providedIn: 'root'
})
export class SessionStorageService {
  private platformId = inject(PLATFORM_ID);

  getItem<T>(key: string): T | null {
    // Ensure code only runs in the browser
    if (!isPlatformBrowser(this.platformId)) {
      return null;
    }

    const item = sessionStorage.getItem(key);
    if (!item) return null;

    try {
      return JSON.parse(item) as T;
    } catch {
      // If it's a plain string that wasn't JSON stringified, return it as-is
      return item as unknown as T;
    }
  }

  setItem(key: string, value: unknown): void {
    if (isPlatformBrowser(this.platformId)) {
      const serialized = typeof value === 'string' ? value : JSON.stringify(value);
      sessionStorage.setItem(key, serialized);
    }
  }
}
