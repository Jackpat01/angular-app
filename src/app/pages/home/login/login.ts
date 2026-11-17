import { Component, inject, OnInit, ChangeDetectionStrategy, signal } from '@angular/core';
import { CommonModule, NgOptimizedImage } from '@angular/common';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { LoginService } from '../../../services/login';
import { PortalConfigService } from '../../../services/portal-config';
import { TempDataService } from '../../../services/temp-data';
import { SessionStorageService } from '../../../services/session-storage-service';
import { AsyncHandlerService } from '../../../services/async-handler.service';
import { BaseFormComponent } from '../../../shared/base-form.component';
import { ARUMUserValidation_Lightweight } from '../../../models/ARUMUserValidation_Lightweight';

@Component({
  imports: [CommonModule, ReactiveFormsModule, NgOptimizedImage],
  selector: 'ohfi-login',
  styleUrl: './login.css',
  templateUrl: './login.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Login extends BaseFormComponent implements OnInit {
  private loginService = inject(LoginService);
  private portalConfig = inject(PortalConfigService);
  private router = inject(Router);
  private tempData = inject(TempDataService);
  private session = inject(SessionStorageService);
  private asyncHandler = inject(AsyncHandlerService);

  isLoading = signal(false);

  override ngOnInit(): void {
    super.ngOnInit();

    this.portalConfig.waitForConfigLoad().catch((error) => {
      console.error('Error loading portal config:', error);
    });

    if (this.session.getItem<boolean>('isSSO')) {
      this.router.navigate(['/sso/error']);
    }

    const message = this.session.getItem<string>('globalErrorMessage');
    if (message) {
      this.tempData.setTempData('errorMessage', { message });
      this.session.removeItem('globalErrorMessage');
    }

    this.form = this.buildForm();
  }

  protected getPageTitle(): string {
    return this.portalConfig.getWebPortalTitle() + ' - Home - Login';
  }

  protected buildForm(): FormGroup {
    return this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

  onSubmit(): void {
    this.submitted = true;

    if (this.session.getItem<boolean>('isSSO')) {
      this.router.navigate(['/sso/error']);
    }

    if (this.form.invalid) {
      return;
    }

    const { email, password } = this.form.value;

    this.asyncHandler.executeAsync(
      this.loginService.loginToPage({ email, password }),
      this.isLoading,
      (response) => {
        if (!response.successful) {
          const message = response.errorMessage;
          this.tempData.setTempData('errorMessage', { message });
          return;
        }

        if (!response.arumUser) {
          const message = 'Username/password invalid';
          this.tempData.setTempData('errorMessage', { message });
          return;
        }

        this.session.setItem('email', response.arumUser.email);
        this.session.setItem('arumUser', JSON.stringify(response.arumUser));

        if (response.arumValidation) {
          const lightweightValidation: ARUMUserValidation_Lightweight = {
            lstARUMUsers: response.arumValidation.lstARUMUsers.map((user) => ({
              token: user.token,
              isActive: user.isActive,
            })),
            results: response.arumValidation.results,
          };
          this.session.setItem('arumUserValidations', JSON.stringify(lightweightValidation));
        }

        if (response.redirectToTerm) {
          this.router.navigate(['/accept-terms']);
        } else {
          this.router.navigate(['/projects']);
        }
      },
      (errorMessage: string) => {
        this.tempData.setTempData('errorMessage', { message: errorMessage });
        console.error('Login error:', errorMessage);
      },
    );
  }

  dismissError(): void {
    this.tempData.removeTempData('errorMessage');
  }
}
