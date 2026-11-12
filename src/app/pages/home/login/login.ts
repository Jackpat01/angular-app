import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TitleService } from '../../../services/title';
import { LoginService } from '../../../services/login';
import { AppConfigService } from '../../../services/app-config';
import { PortalConfigService } from '../../../services/portal-config';

@Component({
  imports: [CommonModule, ReactiveFormsModule],
  selector: 'ohfi-login',
  styleUrl: './login.css',
  templateUrl: './login.html',
})
export class Login implements OnInit {
  private fb = inject(FormBuilder);
  loginForm!: FormGroup;
  submitted = false;
  loginError: string | null = null;
  isLoading = false;
  appTitle = 'OHFI';

  constructor(
    private titleService: TitleService,
    private loginService: LoginService,
    private appConfigService: AppConfigService,
    private portalConfigService: PortalConfigService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle(
      this.appConfigService.getConfig().WebPortalTitle + ' - Home - Login'
    );
    this.initializeForm();
  }

  private initializeForm(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

  get f() {
    return this.loginForm.controls;
  }

  onSubmit(): void {
    this.submitted = true;
    this.loginError = null;

    if (this.loginForm.invalid) {
      return;
    }

    this.isLoading = true;
    const { email, password } = this.loginForm.value;

    this.loginService.loginToPage({ email, password }).subscribe({
      next: (response) => {
        this.isLoading = false;

        if (!response.successful) {
          this.loginError = response.message || 'Login failed. Please try again.';
          return;
        }

        if (!response.lstARUMUsers || response.lstARUMUsers.length === 0) {
          this.loginError = 'Username/password invalid';
          return;
        }

        // Store email in session storage for use in other components
        sessionStorage.setItem('email', email);
        sessionStorage.setItem('arumUsers', JSON.stringify(response.lstARUMUsers));

        // Check if user needs to accept terms
        if (this.loginService.userNeedsToAcceptTerms(response.lstARUMUsers)) {
          this.router.navigate(['/accept-terms']);
        } else {
          // Navigate to projects/dashboard
          this.router.navigate(['/projects']);
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.loginError =
          error.message || 'Login failed. Please check your credentials and try again.';
        console.error('Login error:', error);
      }
    });
  }

  dismissError(): void {
    this.loginError = null;
  }
}
