import {ARUM_User} from './ARUM_User';

export interface ARUMUserWithToken extends ARUM_User{
  token:string;
  isActive:boolean;
}
