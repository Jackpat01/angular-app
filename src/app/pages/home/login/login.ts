import { Component, OnInit, inject } from '@angular/core';
import {CommonModule, NgOptimizedImage} from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TitleService } from '../../../services/title';
import { LoginService } from '../../../services/login';
import { AppConfigService } from '../../../services/app-config';
import { PortalConfigService } from '../../../services/portal-config';
import {TempDataService} from '../../../services/temp-data';

@Component({
  imports: [CommonModule, ReactiveFormsModule, NgOptimizedImage],
  selector: 'ohfi-login',
  styleUrl: './login.css',
  templateUrl: './login.html',
})
export class Login implements OnInit {
  private fb = inject(FormBuilder);
  loginForm!: FormGroup;
  submitted = false;
  isLoading = false;

  constructor(
    private titleService: TitleService,
    private loginService: LoginService,
    private appConfigService: AppConfigService,
    private portalConfig: PortalConfigService,
    private router: Router,
    private tempData:TempDataService
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

    if (this.loginForm.invalid) {
      return;
    }

    this.isLoading = true;
    const { email, password } = this.loginForm.value;

    this.loginService.loginToPage({ email, password }).subscribe({
      next: (response) => {
        this.isLoading = false;

        if (!response.successful) {
          this.tempData.setTempData('errorMessage',response.errorMessage || 'Login failed. Please try again.');
          return;
        }

        if (!response.ARUMUser) {
          this.tempData.setTempData('errorMessage','Username/password invalid');
          return;
        }

        // Store email in session storage for use in other components
        sessionStorage.setItem('email', response.ARUMUser.email);
        sessionStorage.setItem('arumUser', JSON.stringify(response.ARUMUser));

        // Check if user needs to accept terms
        if (response.redirectToTerm) {
          this.router.navigate(['/accept-terms']);
        } else {
          // Navigate to projects/dashboard
          this.router.navigate(['/projects']);
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.tempData.setTempData('errorMessage', error.message || 'Login failed. Please check your credentials and try again.');
        console.error('Login error:', error);
      }
    });
  }

  dismissError(): void {
    this.tempData.removeTempData('errorMessage');
  }
}
