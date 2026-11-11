import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PortalConfigService {
  // Holds the application name; can be updated dynamically via API
  readonly application = signal<string>('My Portal');
}
