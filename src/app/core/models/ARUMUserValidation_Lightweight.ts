import { ARUMResults } from './ARUMResults';

export interface ARUMUserWithToken_Lightweight {
  token: string;
  isActive: boolean;
}

export interface ARUMUserValidation_Lightweight {
  lstARUMUsers: ARUMUserWithToken_Lightweight[];
  results: ARUMResults;
}
