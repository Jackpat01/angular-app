import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Observable } from 'rxjs';
import { TitleService } from '../../services/title';

@Component({
  imports: [CommonModule],
  selector: 'app-header',
  styleUrl: './header.scss',
  templateUrl: './header.html',
})
export class Header implements OnInit {
  title$!: Observable<string>;

  constructor(private titleService: TitleService) {}

  ngOnInit(): void {
    this.title$ = this.titleService.title$;
  }
}
