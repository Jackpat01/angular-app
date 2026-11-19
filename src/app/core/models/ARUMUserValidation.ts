import {ARUMResults} from './ARUMResults';
import {ARUMUserWithToken} from './ARUMUserWithToken';

export interface ARUMUserValidation{
  lstARUMUsers:ARUMUserWithToken[];
  results:ARUMResults;
}
