import {Injectable} from '@angular/core';
import {BehaviorSubject, Observable} from 'rxjs';
import {ApiService} from './api';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
export interface EulaModel {
  EULAText: SafeHtml;
}

@Injectable({providedIn: 'root'})
export class EulaService {
  private modelSubject: BehaviorSubject<EulaModel>;
  model$;

  constructor(private apiService: ApiService, private sanitizer: DomSanitizer) {
    const rawHtml=`
<p style="font-family: Arial, sans-serif; font-weight: bold; text-align: center;">
    <b>Myers and Stauffer xx_State_xx Web Portal<br/>Terms of Use Agreement:</b>
</p>

<p style="font-family: Arial, Helvetica, sans-serif; font-size: 1rem; line-height: 1.5; color: #333;">PLEASE CAREFULLY READ THESE TERMS BEFORE SIGNING THIS AGREEMENT AND USING THIS SITE</p>

<p style="font-family: Arial, Helvetica, sans-serif; font-size: 1rem; line-height: 1.5; color: #333;">Myers and Stauffer LC (MSLC) provides a xx_State_xx Web Portal Application as a service to our clients and their covered entities. Use of the site is strictly limited to clients of Myers and Stauffer and authorized third parties for use during the normal course of our business. As a user of the MSLC Site you are solely and fully responsible for any and all content that you may send to this site. All users of the site are bound by MSLC's Site Terms of Use that are as follows:</p>
<ol>
<li><p style="font-family: Arial, Helvetica, sans-serif; font-size: 1rem; line-height: 1.5; color: #333;">You agree that you will only use this site for business on behalf of the agency you represent as of the date this agreement is executed.</p></li>
<li><p style="font-family: Arial, Helvetica, sans-serif; font-size: 1rem; line-height: 1.5; color: #333;">You will keep your log-in credentials confidential and will not share your log-in credentials with anyone.</p></li>
<li><p style="font-family: Arial, Helvetica, sans-serif; font-size: 1rem; line-height: 1.5; color: #333;">When transmitting files containing protected health information, trade secrets or other proprietary information, you will transmit only the minimum amount necessary to accomplish the project objectives.</p></li>
<li><p style="font-family: Arial, Helvetica, sans-serif; font-size: 1rem; line-height: 1.5; color: #333;">You agree to notify us promptly in the event you no longer require access to the site.</p></li>
<li><p style="font-family: Arial, Helvetica, sans-serif; font-size: 1rem; line-height: 1.5; color: #333;">You agree that you or your agency will notify us immediately in the event you leave the employ of the agency. The agency representative signing below affirms this requirement.</p></li>
<li><p style="font-family: Arial, Helvetica, sans-serif; font-size: 1rem; line-height: 1.5; color: #333;">You agree to not use your log-in credentials, attempt to access the site, or upload any files to or from the site if you leave the employ of the agency.</p></li>
</ol>
<p style="font-family: Arial, Helvetica, sans-serif; font-size: 1rem; line-height: 1.5; color: #333;">MSLC reserves the right to change, modify, suspend or terminate the site at anytime without prior notice.</p>
<p style="font-family: Arial, Helvetica, sans-serif; font-size: 1rem; line-height: 1.5; color: #333;">Myers and Stauffer reserves the right to change or amend these Terms of Use or our Privacy Policy at any time without prior notice. If the terms of this agreement are amended, you will receive notice of the change.</p>
`;

    const defaultModel: EulaModel = {
      EULAText: this.sanitizer.bypassSecurityTrustHtml(rawHtml)
    };

    this.modelSubject = new BehaviorSubject<EulaModel>(defaultModel);
    this.model$ = this.modelSubject.asObservable();

    this.loadModel();
  }

  private loadModel(): void {
    this.apiService.get<EulaModel>('/app/eula').subscribe({
      next: (config) => {
        this.modelSubject.next(config);
      },
      error: (error) => {
        console.error('Failed to load eula model from API', error);
        // Keep using default config on error
      }
    });
  }

  getModel(): EulaModel {
    return this.modelSubject.value;
  }

  getEula(): SafeHtml {
    return this.modelSubject.value.EULAText;
  }

  refreshConfig(): Observable<EulaModel> {
    return new Observable(observer => {
      this.apiService.get<EulaModel>('/app/eula').subscribe({
        next: (config) => {
          this.modelSubject.next(config);
          observer.next(config);
          observer.complete();
        },
        error: (error) => {
          console.error('Failed to refresh eula model', error);
          observer.error(error);
        }
      });
    });
  }

}
