import {Classification} from './Classification';
import {ARUMProxy_User} from './ARUMProxy_User';

export interface AccessFeatureRequest {
  featureName: string;
  classification?: Classification|null;
  arumUser?: ARUMProxy_User|null;
}
