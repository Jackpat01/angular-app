import {ARUMUserProviders} from './ARUMUserProviders';
import {ARUMRole} from './ARUMRole';
import {ARUMResults} from './ARUMResults';
import {ARUMBulletinBoards} from './ARUMBulletinBoards';

export interface ARUM_User{
  acceptedTerms?: Date;
applicationName:string;
bulletinBoardCount:number;
bulletinBoards :ARUMBulletinBoards[];
email:string;
firstName:string;
ipAddress:string;
lastChangePasswordDate?:Date;
lastName :string;
loginErrorCount :number;
phoneNumber:string;
providerCount:number;
providers:ARUMUserProviders[]
results:ARUMResults;
roleCount:number;
roles:ARUMRole[];
title:string;
type :string;
userID :number;
}
