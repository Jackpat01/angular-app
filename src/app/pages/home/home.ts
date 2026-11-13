import {Component, OnInit, inject} from '@angular/core';
import {RouterOutlet} from '@angular/router';
import {TitleService} from '../../services/title';

@Component({
  imports: [RouterOutlet],
  selector: 'ohfi-home',
  styleUrl: './home.css',
  templateUrl: './home.html',
})
export class Home implements OnInit {
  private titleService = inject(TitleService);

  ngOnInit(): void {
    this.titleService.setTitle('Home');
  }
}
