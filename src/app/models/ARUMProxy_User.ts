import {ARUMUserProviders} from './ARUMUserProviders';
import {ARUMRole} from './ARUMRole';

export interface ARUMProxy_User {
  userID: number;
  email: string;
  acceptedTerms: boolean | null;
  token: string;
  ipAddress: string;
  providers: ARUMUserProviders[];
  roles:ARUMRole[];
}
