import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Observable } from 'rxjs';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { TitleService } from '../../services/title';

@Component({
  imports: [CommonModule, NgbModule],
  selector: 'ohfi-header',
  styleUrl: './header.scss',
  templateUrl: './header.html',
})
export class Header implements OnInit {
  title$!: Observable<string>;
  navbarOpen = false;

  constructor(private titleService: TitleService) {}

  ngOnInit(): void {
    this.title$ = this.titleService.title$;
  }

  toggleNavbar(): void {
    this.navbarOpen = !this.navbarOpen;
  }
}
