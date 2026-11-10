import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TitleService } from '../../services/title';
import { UserService } from '../../services/user';

@Component({
  imports: [CommonModule, ReactiveFormsModule],
  selector: 'app-login',
  styleUrl: './login.scss',
  templateUrl: './login.html',
})
export class Login implements OnInit {
  loginForm!: FormGroup;
  submitted = false;
  loginError: string | null = null;

  constructor(
    private formBuilder: FormBuilder,
    private titleService: TitleService,
    private userService: UserService
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle('Login');
    this.initializeForm();
  }

  private initializeForm(): void {
    this.loginForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [false],
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

    const { email, password } = this.loginForm.value;

    if (email === 'admin@example.com' && password === 'admin123') {
      this.userService.setUser({
        email: email,
        role: 'Administrator',
        providerType: 'Admin',
        projectName: 'Admin Portal',
        isAuthenticated: true,
      });
      window.location.href = '/admin';
    } else if (email === 'user@example.com' && password === 'user123') {
      this.userService.setUser({
        email: email,
        role: 'User',
        providerType: 'Standard',
        projectName: 'User Dashboard',
        isAuthenticated: true,
      });
      window.location.href = '/user-dashboard';
    } else {
      this.loginError = 'Invalid email or password. Try admin@example.com/admin123 or user@example.com/user123';
    }
  }
}
