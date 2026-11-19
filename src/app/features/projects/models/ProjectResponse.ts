import {Classification} from '../../../core/models/Classification';
import {ARUM_User} from '../../../core/models/ARUM_User';

export interface ProjectResponse{
  successful: boolean;
  message: string;
  path:string;
  classification:Classification;
  arumUser:ARUM_User;
}
