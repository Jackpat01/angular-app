import {Component} from '@angular/core';
import {CommonModule} from '@angular/common';
import {TitleService} from '../../services/title';

@Component({
  imports: [CommonModule],
  selector: 'ohfi-page-title-banner',
  template: `
    <div class="page-title-banner">
      <h1>{{ (titleService.title$ | async) || 'Loading...' }}</h1>
    </div>
  `,
  styles: [`
    .page-title-banner {
      background: linear-gradient(135deg, #66bc29 0%, #5a9f24 100%);
      color: white;
      padding: 24px 20px;
      margin: 0;
      border-bottom: 4px solid #459E00;
    }

    .page-title-banner h1 {
      margin: 0;
      font-size: 28px;
      font-weight: 600;
      letter-spacing: 0.5px;
    }

    @media (max-width: 768px) {
      .page-title-banner {
        padding: 16px 20px;
      }

      .page-title-banner h1 {
        font-size: 20px;
      }
    }
  `]
})
export class PageTitleBannerComponent {
  constructor(readonly titleService: TitleService) {
  }
}
