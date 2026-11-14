import {Classification} from './Classification';
import {ARUM_User} from './ARUM_User';

export interface ProjectResponse{
  successful: boolean;
  message: string;
  path:string;
  classification:Classification;
  arumUser:ARUM_User;
}
