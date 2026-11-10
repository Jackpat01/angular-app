import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TitleService } from '../../services/title';

@Component({
  imports: [RouterOutlet],
  selector: 'ohfi-home',
  styleUrl: './home.scss',
  templateUrl: './home.html',
})
export class Home implements OnInit {
  constructor(private titleService: TitleService) {}

  ngOnInit(): void {
    this.titleService.setTitle('Home');
  }
}
