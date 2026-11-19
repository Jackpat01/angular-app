import {ARUMUserValidation} from '../../../core/models/ARUMUserValidation';

export interface AcceptTermsResponse {
  successful: boolean;
  message: string;
  arumValidation: ARUMUserValidation;
}
