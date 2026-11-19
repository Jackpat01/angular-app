import { Injectable } from '@angular/core';
import { BehaviorSubject, firstValueFrom, Observable } from 'rxjs';
import { filter, skip } from 'rxjs/operators';

@Injectable()
export abstract class BaseConfigService<C> {
  protected readonly defaultValue: C;
  protected readonly endpoint: string;

  protected configSubject: BehaviorSubject<C>;
  protected configLoadedSubject = new BehaviorSubject<boolean>(false);

  config$: Observable<C>;
  configLoaded$: Observable<boolean>;

  // eslint-disable-next-line @angular-eslint/prefer-inject
  constructor(config: { defaultValue: C; endpoint: string }) {
    this.defaultValue = config.defaultValue;
    this.endpoint = config.endpoint;
    this.configSubject = new BehaviorSubject<C>(this.defaultValue);
    this.config$ = this.configSubject.asObservable();
    this.configLoaded$ = this.configLoadedSubject.asObservable();
  }

  isConfigLoaded(): boolean {
    return this.configLoadedSubject.value;
  }

  async waitForConfigLoad(): Promise<void> {
    if (this.isConfigLoaded()) {
      return;
    }
    await firstValueFrom(this.configLoaded$.pipe(
      skip(1),
      filter(loaded => loaded === true)
    ));
  }

  getConfig(): C {
    return this.configSubject.value;
  }

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

  protected abstract loadFromApi(): Observable<C>;

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
