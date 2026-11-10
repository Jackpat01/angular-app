import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TitleService } from '../../services/title';

@Component({
  imports: [CommonModule, RouterOutlet],
  selector: 'app-public-layout',
  styleUrl: './public-layout.scss',
  templateUrl: './public-layout.html',
})
export class PublicLayout implements OnInit {
  appVersion = '1.0.0';
  currentYear = new Date().getFullYear();

  constructor(private titleService: TitleService) {}

  ngOnInit(): void {
    this.titleService.setTitle('Welcome');
  }
}

