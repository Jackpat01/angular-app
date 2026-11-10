import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TitleService } from '../../../services/title';
import { UserService } from '../../../services/user';
import { AppConfigService } from '../../../services/app-config';

@Component({
  imports: [CommonModule, ReactiveFormsModule],
  selector: 'ohfi-login',
  styleUrl: './login.scss',
  templateUrl: './login.html',
})
export class Login implements OnInit {
  loginForm!: FormGroup;
  submitted = false;
  loginError: string | null = null;
  isLoading = false;
  appTitle = 'OHFI';

  constructor(
    private formBuilder: FormBuilder,
    private titleService: TitleService,
    private userService: UserService,
    private appConfigService: AppConfigService
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle(this.appConfigService.getConfig().WebPortalTitle + ' - Home - Login');
    this.initializeForm();
  }

  private initializeForm(): void {
    this.loginForm = this.formBuilder.group({
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
    const credentials = { email, password };

    this.userService.login(credentials).subscribe({
      next: (response) => {
        this.userService.setUser(response.user);
        this.userService.setAuthenticated(true);
        this.isLoading = false;
        // Redirect based on user role
        if (response.user.role === 'Administrator') {
          window.location.href = '/admin';
        } else {
          window.location.href = '/user-dashboard';
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.loginError = error.message || 'Login failed. Please try again.';
      }
    });
  }

  dismissError(): void {
    this.loginError = null;
  }
}
