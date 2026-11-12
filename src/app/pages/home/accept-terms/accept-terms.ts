import { Component, inject, OnInit } from '@angular/core';
import {CommonModule, NgOptimizedImage} from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TitleService } from '../../../services/title';
import { PortalConfigService } from '../../../services/portal-config';
import { LoginService } from '../../../services/login';

@Component({
  selector: 'ohfi-accept-terms',
  templateUrl: './accept-terms.html',
  styleUrl: './accept-terms.css',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, NgOptimizedImage],
})
export class AcceptTerms implements OnInit {
  private titleService = inject(TitleService);
  private portalConfig = inject(PortalConfigService);
  private loginService = inject(LoginService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  acceptForm: FormGroup = this.fb.group({
    acceptCheckbox: [false, [Validators.requiredTrue]],
  });

  eulaText: string = '';
  isLoading: boolean = false;
  acceptError: string = '';
  submitted: boolean = false;
  appTitle: string = '';
  email: string = '';

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
    this.loginService.getEULAText(this.email).subscribe({
      next: (response) => {
        this.eulaText = response.eulaText;
        this.isLoading = false;
      },
      error: (error) => {
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
      error: (error) => {
        this.acceptError = 'An error occurred while accepting terms. Please try again.';
        this.isLoading = false;
      },
    });
  }

  dismissError(): void {
    this.acceptError = '';
  }

  get f() {
    return this.acceptForm.controls;
  }
}
