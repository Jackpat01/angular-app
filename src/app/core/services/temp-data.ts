import {Injectable} from '@angular/core';
import {BehaviorSubject, Observable} from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class TempDataService {
  private tempDataSubject = new BehaviorSubject<Map<string, Record<string, unknown>>>(new Map());
  tempData$: Observable<Map<string, Record<string, unknown>>> = this.tempDataSubject.asObservable();

  setTempData(key: string, data: Record<string, unknown>): void {
    const currentData = this.tempDataSubject.getValue();
    currentData.set(key, data);
    this.tempDataSubject.next(currentData);
  }

  consume(key: string): Record<string, unknown> | null  {
    const value = this.getTempData(key);
    if (value !== null) {
      this.removeTempData(key);
    }
    return value;
  }

  getTempData(key: string): Record<string, unknown> | null {
    const data = this.tempDataSubject.getValue().get(key);
    return data !== undefined ? data : null;
  }

  peek(key: string): Record<string, unknown> | null {
    const currentData = this.tempDataSubject.getValue();
    const data = currentData.get(key);

    if (data !== undefined) {
      currentData.delete(key);
      this.tempDataSubject.next(currentData);
      return data;
    }

    return null;
  }

  hasTempData(key: string): boolean {
    return this.tempDataSubject.getValue().has(key);
  }

  clearTempData(): void {
    this.tempDataSubject.next(new Map());
  }

  removeTempData(key: string): void {
    const currentData = this.tempDataSubject.getValue();
    currentData.delete(key);
    this.tempDataSubject.next(currentData);
  }

  getAllKeys(): string[] {
    return Array.from(this.tempDataSubject.getValue().keys());
  }
}
