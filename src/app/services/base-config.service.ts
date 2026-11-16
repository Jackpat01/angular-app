import { Injectable } from '@angular/core';
import { BehaviorSubject, firstValueFrom, Observable, Subject } from 'rxjs';
import { filter, skip } from 'rxjs/operators';

/**
 * Base class for configuration services that load data from an API.
 * Provides common patterns for async config loading, caching, and observability.
 * 
 * Type parameter C: Configuration interface
 * 
 * Example usage:
 * ```
 * @Injectable({ providedIn: 'root' })
 * export class MyConfigService extends BaseConfigService<MyConfig> {
 *   constructor(private apiService: ApiService) {
 *     super({
 *       defaultValue: { ... },
 *       endpoint: '/api/my-config'
 *     });
 *   }
 *   
 *   protected loadFromApi(): Observable<MyConfig> {
 *     return this.apiService.get<MyConfig>(this.endpoint);
 *   }
 * }
 * ```
 */
@Injectable()
export abstract class BaseConfigService<C> {
  protected readonly defaultValue: C;
  protected readonly endpoint: string;

  protected configSubject: BehaviorSubject<C>;
  protected configLoadedSubject = new BehaviorSubject<boolean>(false);

  config$: Observable<C>;
  configLoaded$: Observable<boolean>;

  constructor(config: { defaultValue: C; endpoint: string }) {
    this.defaultValue = config.defaultValue;
    this.endpoint = config.endpoint;
    this.configSubject = new BehaviorSubject<C>(this.defaultValue);
    this.config$ = this.configSubject.asObservable();
    this.configLoaded$ = this.configLoadedSubject.asObservable();
  }

  /**
   * Check if config has been loaded from API
   */
  isConfigLoaded(): boolean {
    return this.configLoadedSubject.value;
  }

  /**
   * Wait for config to load from API
   */
  async waitForConfigLoad(): Promise<void> {
    if (this.isConfigLoaded()) {
      return;
    }
    return firstValueFrom(this.configLoaded$.pipe(
      skip(1),
      filter(loaded => loaded === true)
    )).then(() => {});
  }

  /**
   * Get the current configuration value (never null)
   */
  getConfig(): C {
    return this.configSubject.value;
  }

  /**
   * Refresh configuration from API
   * Returns an Observable that emits the new config
   */
  refreshConfig(): Observable<C> {
    return new Observable(observer => {
      this.loadFromApi().subscribe({
        next: (config) => {
          this.configSubject.next(config);
          this.configLoadedSubject.next(true);
          observer.next(config);
          observer.complete();
        },
        error: (error) => {
          console.error(`Failed to refresh config from ${this.endpoint}`, error);
          observer.error(error);
        }
      });
    });
  }

  /**
   * Override this method to provide custom API loading logic
   */
  protected abstract loadFromApi(): Observable<C>;

  /**
   * Load configuration from API and handle errors
   * This is called automatically in the constructor
   */
  protected loadConfig(): void {
    this.loadFromApi().subscribe({
      next: (config) => {
        this.configSubject.next(config);
        this.configLoadedSubject.next(true);
      },
      error: (error) => {
        console.error(`Failed to load config from ${this.endpoint}`, error);
        console.warn('Using default configuration');
        this.configLoadedSubject.next(true);
      }
    });
  }
}
