import {Component, OnInit, signal} from '@angular/core';
import { CommonModule } from '@angular/common';
import { TitleService } from '../../services/title';
import { AppConfigService, AppConfig } from '../../services/app-config';
import {RouterLink, RouterOutlet} from '@angular/router';

@Component({
  imports: [CommonModule, RouterOutlet, RouterLink],
  selector: 'ohfi-base-layout',
  styleUrl: './base-layout.css',
  templateUrl: './base-layout.html',
})
export class BaseLayout implements OnInit {
  appConfig: AppConfig | null = null;
  lineArray = signal<string[]>([]);
  version = '1.0.0';

  constructor(
    private titleService: TitleService,
    private appConfigService: AppConfigService
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle('Welcome');
    this.appConfig = this.appConfigService.getConfig();
  }

  get appVersion(): string {
    return this.appConfig?.appVersion || '';
  }

  get currentYear(): number {
    return this.appConfig?.currentYear || new Date().getFullYear();
  }
}

