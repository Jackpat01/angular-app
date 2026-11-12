import {Injectable} from '@angular/core';
import {BehaviorSubject, Observable} from 'rxjs';

/**
 * TempDataService provides MVC TempData-like functionality for Angular
 * Stores temporary data that is automatically cleared after first consumption
 * Use for passing data between route navigations within the same session
 */
@Injectable({
  providedIn: 'root',
})
export class TempDataService {
  private tempDataSubject = new BehaviorSubject<Map<string, any>>(new Map());
  tempData$: Observable<Map<string, any>> = this.tempDataSubject.asObservable();

  /**
   * Set temporary data with a key
   * @param key - Unique identifier for the data
   * @param data - Any data to store
   */
  setTempData(key: string, data: any): void {
    const currentData = this.tempDataSubject.getValue();
    currentData.set(key, data);
    this.tempDataSubject.next(currentData);
  }

  /**
   * Get temporary data by key
   * Does not remove the data; use peek() for that
   * @param key - Key of the data to retrieve
   * @returns The stored data or null if not found
   */
  getTempData(key: string): any {
    const data = this.tempDataSubject.getValue().get(key);
    return data !== undefined ? data : null;
  }

  /**
   * Get temporary data and remove it from storage
   * Mimics MVC TempData behavior where data is consumed after reading
   * @param key - Key of the data to retrieve
   * @returns The stored data or null if not found
   */
  peek(key: string): any {
    const currentData = this.tempDataSubject.getValue();
    const data = currentData.get(key);

    if (data !== undefined) {
      currentData.delete(key);
      this.tempDataSubject.next(currentData);
      return data;
    }

    return null;
  }

  /**
   * Check if a key exists in temporary data
   * @param key - Key to check
   * @returns true if key exists
   */
  hasTempData(key: string): boolean {
    return this.tempDataSubject.getValue().has(key);
  }

  /**
   * Clear all temporary data
   */
  clearTempData(): void {
    this.tempDataSubject.next(new Map());
  }

  /**
   * Remove a specific temporary data by key
   * @param key - Key to remove
   */
  removeTempData(key: string): void {
    const currentData = this.tempDataSubject.getValue();
    currentData.delete(key);
    this.tempDataSubject.next(currentData);
  }

  /**
   * Get all keys in temporary data
   * @returns Array of all stored keys
   */
  getAllKeys(): string[] {
    return Array.from(this.tempDataSubject.getValue().keys());
  }
}
