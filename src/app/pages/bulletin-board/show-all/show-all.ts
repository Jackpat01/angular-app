import {ChangeDetectionStrategy, Component, inject, OnInit} from '@angular/core';
import {DatePipe} from '@angular/common';
import {BulletinBoardFacade} from '../bulletin-board.facade';

@Component({
  imports: [DatePipe],
//  selector: 'ohfi-show-all',
  styleUrl: './show-all.css',
  templateUrl: './show-all.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShowAll implements OnInit {
  protected readonly facade = inject(BulletinBoardFacade);

  ngOnInit(): void {
    this.facade.setPageTitle();
    this.facade.loadFromSession();
  }
}
