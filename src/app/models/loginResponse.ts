import {ARUMProxy_User} from './ARUMProxy_User';

export interface LoginResponse {
  successful: boolean;
  redirectToTerm: boolean;
  errorMessage: string;
  userName: string;
  email: string;
  arumUser?: ARUMProxy_User;
}
