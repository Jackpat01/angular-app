import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TitleService } from '../../../services/title';

@Component({
  imports: [],
  selector: 'ohfi-error',
  styleUrl: './error.scss',
  templateUrl: './error.html',
})
export class ErrorPage implements OnInit {
  errorCode: number = 404;
  errorMessage: string = 'Page Not Found';
  errorDescription: string = 'The page you are looking for does not exist.';

  constructor(
    private route: ActivatedRoute,
    private titleService: TitleService
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle('Error');

    this.route.queryParams.subscribe((params) => {
      if (params['code']) {
        this.errorCode = parseInt(params['code'], 10);
      }
      if (params['message']) {
        this.errorMessage = params['message'];
      }
      if (params['description']) {
        this.errorDescription = params['description'];
      }
    });
  }

  goBack(): void {
    window.history.back();
  }

  goHome(): void {
    window.location.href = '/';
  }
}

