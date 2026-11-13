import {inject, Injectable} from '@angular/core';
import {ARUMUserValidation} from '../models/ARUMUserValidation';
import {SessionStorageService} from './session-storage-service';

@Injectable({
  providedIn: 'root',
})
export class FunctionsService {
  session = inject(SessionStorageService);

  getValidations(): ARUMUserValidation | null {
    if (this.session.getItem('ARUMUserValidations') != null) {
      return this.session.getItem<ARUMUserValidation>('ARUMUserValidations');
    }

    return null;
  }
}
