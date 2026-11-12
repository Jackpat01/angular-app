import {Injectable, OnDestroy} from '@angular/core';
import {interval, Subscription} from 'rxjs';
import {ApiService} from './api';

@Injectable({
  providedIn: 'root',
})
export class HeartbeatService implements OnDestroy {
  private heartbeatInterval: number = 5 * 60 * 1000; // 5 minutes
  private heartbeatSubscription: Subscription | null = null;

  constructor(private apiService: ApiService) {
  }

  /**
   * Start sending heartbeat to server at regular intervals
   */
  startHeartbeat(): void {
    if (this.heartbeatSubscription) {
      return; // Already running
    }

    this.heartbeatSubscription = interval(this.heartbeatInterval).subscribe(() => {
      this.sendHeartbeat();
    });
  }

  /**
   * Stop sending heartbeat
   */
  stopHeartbeat(): void {
    if (this.heartbeatSubscription) {
      this.heartbeatSubscription.unsubscribe();
      this.heartbeatSubscription = null;
    }
  }

  /**
   * Set custom heartbeat interval (in milliseconds)
   */
  setHeartbeatInterval(intervalMs: number): void {
    this.stopHeartbeat();
    this.heartbeatInterval = intervalMs;
    this.startHeartbeat();
  }

  /**
   * Get current heartbeat interval
   */
  getHeartbeatInterval(): number {
    return this.heartbeatInterval;
  }

  ngOnDestroy(): void {
    this.stopHeartbeat();
  }

  /**
   * Send heartbeat to server
   * Endpoint: POST /heartbeat
   */
  private sendHeartbeat(): void {
    this.apiService.post('/heartbeat', {}).subscribe({
      next: () => {
        console.debug('Heartbeat sent successfully');
      },
      error: (error) => {
        console.error('Heartbeat failed:', error);
      }
    });
  }
}
