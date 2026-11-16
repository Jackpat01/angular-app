import {Classification} from './Classification';

export interface PortalConfig {
  application: string;
  loginMessage: string;
  webPortalTitle: string;
  classificationSessionKey:string;
  allClassifications:Classification[];
  classificationReplacementTerm:string;
  enableBB:boolean;
  enableResource:boolean;
  auditTypeReplacementName:string;

  [key: string]: unknown;
}
