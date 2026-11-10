import { Component, OnInit } from '@angular/core';
import { TitleService } from '../../services/title';

@Component({
  imports: [],
  selector: 'ohfi-user-dashboard',
  styleUrl: './user-dashboard.scss',
  templateUrl: './user-dashboard.html',
})
export class UserDashboard implements OnInit {
  constructor(private titleService: TitleService) {}

  ngOnInit(): void {
    this.titleService.setTitle('User Dashboard');
  }
}
