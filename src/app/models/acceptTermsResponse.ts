import {ARUMUserValidation} from './ARUMUserValidation';

export interface AcceptTermsResponse {
  successful: boolean;
  message: string;
  arumValidations: ARUMUserValidation;
}
