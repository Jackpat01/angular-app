import {ARUMProxy_User} from './ARUMProxy_User';

export interface SetProjectRequest {
  id:string;
  arumUser:ARUMProxy_User;
  isSSOUser:boolean;
}
