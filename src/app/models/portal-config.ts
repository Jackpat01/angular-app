import {Classification} from './Classification';

export interface PortalConfig {
  applicationName: string;
  loginMessageHyperLink: string;
  WebPortalTitle: string;
  loginSessionKey:string;
  allClassifications:Classification[];
  classificationReplacementTerm:string;
  enableBB:boolean;
  enableResource:boolean;
  auditTypeReplacementName:string;

  [key: string]: unknown;
}
