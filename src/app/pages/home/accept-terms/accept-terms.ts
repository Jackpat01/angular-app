import {Component, inject, input, OnInit} from '@angular/core';
import {TitleService} from '../../../services/title';
import {AppConfigService} from '../../../services/app-config';
import {PortalConfigService} from '../../../services/portal-config';
import {FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {toSignal} from '@angular/core/rxjs-interop';
import {EulaService} from '../../../services/eula-service';
import {NgOptimizedImage} from '@angular/common';

@Component({
  imports: [
    FormsModule,
    ReactiveFormsModule,
    NgOptimizedImage
  ],
  selector: 'ohfi-accept-terms',
  styleUrl: './accept-terms.css',
  templateUrl: './accept-terms.html',
})
export class AcceptTerms implements OnInit {
  protected readonly portalConfig = inject(PortalConfigService);
  private fb = inject(FormBuilder);
  protected readonly eula = inject(EulaService);
  acceptForm: FormGroup = this.fb.group({
    emailControl: ['', [Validators.required, Validators.email]],
    nonHashedPasswordControl: ['', [Validators.required, Validators.minLength(6)]],
  });

  constructor(
    private titleService: TitleService
  ) {
  }

  ngOnInit(): void {
    this.titleService.setTitle(this.portalConfig.getConfig().applicationName + ' - Home - Accept Terms of Use');
    this.eula.refreshConfig();
  }

  onSubmit(): void {

  }
}
