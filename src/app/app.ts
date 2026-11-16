import {Component, DOCUMENT, inject, Renderer2} from '@angular/core';
import {ActivatedRoute, NavigationEnd, Router, RouterOutlet} from '@angular/router';
import {filter} from 'rxjs/operators';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';

@Component({
  imports: [RouterOutlet],
  selector: 'ohfi-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);
  private document = inject(DOCUMENT);
  private renderer = inject(Renderer2);
  private currentClass = '';

  constructor() {
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      takeUntilDestroyed()
    ).subscribe(() => {
      // Traverse to the deepest active child route
      let route = this.activatedRoute.firstChild;
      while (route?.firstChild) {
        route = route.firstChild;
      }

      const data = route?.snapshot.data ?? {};

      if (this.currentClass) {
        this.renderer.removeClass(this.document.body, this.currentClass);
      }
      if (data['bodyClass']) {
        this.currentClass = data['bodyClass'];
        this.renderer.addClass(this.document.body, this.currentClass);
      }
    });
  }
}
