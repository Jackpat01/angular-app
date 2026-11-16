import {Component, inject, OnInit, ChangeDetectionStrategy, signal} from '@angular/core';
import {CommonModule, NgOptimizedImage} from '@angular/common';
import {FormBuilder, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {Router} from '@angular/router';
import {TitleService} from '../../../services/title';
import {LoginService} from '../../../services/login';
import {PortalConfigService} from '../../../services/portal-config';
import {TempDataService} from '../../../services/temp-data';
import {SessionStorageService} from '../../../services/session-storage-service';
import {AsyncHandlerService} from '../../../services/async-handler.service';

@Component({
  imports: [CommonModule, ReactiveFormsModule, NgOptimizedImage],
  selector: 'ohfi-login',
  styleUrl: './login.css',
  templateUrl: './login.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Login implements OnInit {
  private titleService = inject(TitleService);
  private loginService = inject(LoginService);
  private portalConfig = inject(PortalConfigService);
  private router = inject(Router);
  private tempData = inject(TempDataService);
  private fb = inject(FormBuilder);
  private session = inject(SessionStorageService);
  private asyncHandler = inject(AsyncHandlerService);

  loginForm!: FormGroup;
  submitted = false;
  isLoading = signal(false);

  get f() {
    return this.loginForm.controls;
  }

  ngOnInit(): void {
    // Wait for portal config to load before setting title
    this.portalConfig.waitForConfigLoad().then(() => {
      this.titleService.setTitle(
        this.portalConfig.getWebPortalTitle() + ' - Home - Login'
      );
    }).catch(error => {
      console.error('Error loading portal config:', error);
      this.titleService.setTitle('OMES - Home - Login');
    });

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

    const {email, password} = this.loginForm.value;

    this.asyncHandler.executeAsync(
      this.loginService.loginToPage({email, password}),
      this.isLoading,
      (response) => {
        if (!response.successful) {
          let message = response.errorMessage;
          this.tempData.setTempData('errorMessage', {message});
          return;
        }

        if (!response.arumUser) {
          let message = 'Username/password invalid';
          this.tempData.setTempData('errorMessage', {message});
          return;
        }

        this.session.setItem('email', response.arumUser.email);
        this.session.setItem('arumUser', JSON.stringify(response.arumUser));

        if (response.redirectToTerm) {
          this.router.navigate(['/accept-terms']);
        } else {
          this.router.navigate(['/projects']);
        }
      },
      (error) => {
        this.tempData.setTempData('errorMessage', error.message || 'Login failed. Please check your credentials and try again.');
        console.error('Login error:', error);
      }
    );
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
