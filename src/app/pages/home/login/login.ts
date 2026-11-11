import {Component, OnInit, inject, signal} from '@angular/core';
import {FormBuilder, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {TitleService} from '../../../services/title';
import {AppConfigService} from '../../../services/app-config';
import {PortalConfigService} from '../../../services/portal-config';

@Component({
  imports: [ReactiveFormsModule],
  selector: 'ohfi-login',
  styleUrl: './login.css',
  templateUrl: './login.html',
})
export class Login implements OnInit {
  private fb = inject(FormBuilder);
  message = signal<string | null>(null);
  errorMessage = signal<string | null>(null);
  protected readonly portalConfig = inject(PortalConfigService);

  loginForm: FormGroup = this.fb.group({
    emailControl: ['',  [Validators.required, Validators.email]],
    nonHashedPasswordControl: ['', [Validators.required, Validators.minLength(6)]],
  });

  constructor(
    private titleService: TitleService,
    private appConfigService: AppConfigService
  ) {
  }

  ngOnInit(): void {
    this.titleService.setTitle(this.appConfigService.getConfig().WebPortalTitle + ' - Home - Login');
    this.portalConfig.refreshConfig();
  }


  onSubmit(): void {

  }
}
