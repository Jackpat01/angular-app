import {Component, inject, OnInit} from '@angular/core';
import {CommonModule, NgOptimizedImage} from '@angular/common';
import {FormBuilder, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {Router} from '@angular/router';
import {TitleService} from '../../../services/title';
import {LoginService} from '../../../services/login';
import {PortalConfigService} from '../../../services/portal-config';
import {TempDataService} from '../../../services/temp-data';
import {SessionStorageService} from '../../../services/session-storage-service';

@Component({
  imports: [CommonModule, ReactiveFormsModule, NgOptimizedImage],
  selector: 'ohfi-login',
  styleUrl: './login.css',
  templateUrl: './login.html',
})
export class Login implements OnInit {
  private titleService = inject(TitleService);
  private loginService = inject(LoginService);
  private portalConfig = inject(PortalConfigService);
  private router = inject(Router);
  private tempData = inject(TempDataService);
  private fb = inject(FormBuilder);
  private session = inject(SessionStorageService);

  loginForm!: FormGroup;
  submitted = false;
  isLoading = false;

  get f() {
    return this.loginForm.controls;
  }

  ngOnInit(): void {
    this.titleService.setTitle(
      this.portalConfig.getWebPortalTitle() + ' - Home - Login'
    );

    if (this.session.getItem<boolean>('IsSSO')) {
      this.router.navigate(['/sso/error']);
    }

    let message = this.session.getItem<string>('GlobalErrorMessage');
    if (message) {
      this.tempData.setTempData('errorMessage', {message});
      this.session.removeItem('GlobalErrorMessage');
    }

    this.initializeForm();
  }

  onSubmit(): void {
    this.submitted = true;

    if (this.session.getItem<boolean>('IsSSO')) {
      this.router.navigate(['/sso/error']);
    }


    if (this.loginForm.invalid) {
      return;
    }

    this.isLoading = true;
    const {email, password} = this.loginForm.value;

    this.loginService.loginToPage({email, password}).subscribe({
      next: (response) => {
        this.isLoading = false;

        if (!response.successful) {
          let message = response.errorMessage;
          this.tempData.setTempData('errorMessage', {message});
          return;
        }


        if (!response.ARUMUser) {
          let message = 'Username/password invalid';
          this.tempData.setTempData('errorMessage', {message});
          return;
        }

        // Store email in session storage for use in other components
        this.session.setItem('email', response.ARUMUser.email);
        this.session.setItem('arumUser', JSON.stringify(response.ARUMUser));

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

  private initializeForm(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }
}
