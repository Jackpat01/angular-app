import {ARUMProxy_User} from './ARUMProxy_User';
import {ARUMUserValidation} from './ARUMUserValidation';

export interface LoginResponse {
  successful: boolean;
  redirectToTerm: boolean;
  errorMessage: string;
  userName: string;
  email: string;
  arumUser?: ARUMProxy_User;
  arumValidation?: ARUMUserValidation
}
