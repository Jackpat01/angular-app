import { inject, Injectable } from '@angular/core';
import { ARUMUserValidation_Lightweight } from '../models/ARUMUserValidation_Lightweight';
import { SessionStorageService } from './session-storage-service';

@Injectable({
  providedIn: 'root',
})
export class FunctionsService {
  session = inject(SessionStorageService);

  getValidations(): ARUMUserValidation_Lightweight | null {
    if (this.session.getItem('arumUserValidations') != null) {
      return this.session.getItem<ARUMUserValidation_Lightweight>('arumUserValidations');
    }

    return null;
  }
}
