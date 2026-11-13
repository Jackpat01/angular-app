import {Component, inject, OnInit} from '@angular/core';
import {CommonModule, NgOptimizedImage} from '@angular/common';
import {FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {Router} from '@angular/router';
import {TitleService} from '../../../services/title';
import {PortalConfigService} from '../../../services/portal-config';
import {LoginService} from '../../../services/login';

@Component({
  selector: 'ohfi-accept-terms',
  templateUrl: './accept-terms.html',
  styleUrl: './accept-terms.css',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, NgOptimizedImage],
})
export class AcceptTerms implements OnInit {
  eulaText = `
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
  isLoading = false;
  acceptError = '';
  submitted = false;
  appTitle = '';
  email = '';
  private titleService = inject(TitleService);
  private portalConfig = inject(PortalConfigService);
  private loginService = inject(LoginService);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  acceptForm: FormGroup = this.fb.group({
    acceptCheckbox: [false, [Validators.requiredTrue]],
  });

  get f() {
    return this.acceptForm.controls;
  }

  ngOnInit(): void {
    this.appTitle = this.portalConfig.getConfig().applicationName;
    this.titleService.setTitle(`${this.appTitle} - Accept Terms of Use`);

    // Get email from sessionStorage
    this.email = sessionStorage.getItem('userEmail') || '';

    if (!this.email) {
      // No email in session, redirect to login
      this.router.navigate(['/login']);
      return;
    }

    this.loadEULA();
  }

  loadEULA(): void {
    this.isLoading = true;
    this.loginService.getEULAText().subscribe({
      next: (response) => {
        this.eulaText = response.eulaText;
        this.isLoading = false;
      },
      error: () => {
        this.acceptError = 'Failed to load Terms of Use. Please try again.';
        this.isLoading = false;
      },
    });
  }

  onSubmit(): void {
    this.submitted = true;

    if (this.acceptForm.invalid) {
      return;
    }

    this.isLoading = true;
    this.acceptError = '';

    this.loginService.acceptTermsOfUse(this.email).subscribe({
      next: (response) => {
        if (response.successful) {
          // Clear session data
          sessionStorage.removeItem('userEmail');
          // Redirect to projects/home
          this.router.navigate(['/projects']);
        } else {
          this.acceptError = response.message || 'Failed to accept terms. Please try again.';
        }
        this.isLoading = false;
      },
      error: () => {
        this.acceptError = 'An error occurred while accepting terms. Please try again.';
        this.isLoading = false;
      },
    });
  }

  dismissError(): void {
    this.acceptError = '';
  }
}
