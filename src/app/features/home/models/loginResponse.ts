import {ARUMProxy_User} from '../../../core/models/ARUMProxy_User';
import {ARUMUserValidation} from '../../../core/models/ARUMUserValidation';

export interface LoginResponse {
  successful: boolean;
  redirectToTerm: boolean;
  errorMessage: string;
  userName: string;
  email: string;
  arumUser?: ARUMProxy_User;
  arumValidation?: ARUMUserValidation
}
