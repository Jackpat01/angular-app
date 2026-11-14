import {Classification} from './Classification';

export interface PortalConfig {
  applicationName: string;
  loginMessageHyperLink: string;
  WebPortalTitle: string;
  loginSessionKey:string;
  allClassifications:Classification[];
  classificationReplacementTerm:string;
  enableBB:boolean;

  [key: string]: unknown;
}
