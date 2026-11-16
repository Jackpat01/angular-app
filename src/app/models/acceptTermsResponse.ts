import {ARUMUserValidation} from './ARUMUserValidation';

export interface AcceptTermsResponse {
  successful: boolean;
  message: string;
  arumValidation: ARUMUserValidation;
}
