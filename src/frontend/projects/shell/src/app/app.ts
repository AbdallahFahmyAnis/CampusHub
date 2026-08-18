import { Component, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { SessionService } from './session';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  readonly session = inject(SessionService);
  readonly menuOpen = signal(false);
  readonly bleed = signal(false);

  constructor() {
    void this.session.load();
    inject(Router).events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe((event) => {
      this.menuOpen.set(false);
      const url = event.urlAfterRedirects.split('?')[0];
      const courseLanding = /\/catalog\/[0-9a-fA-F-]{36}$/.test(url);
      this.bleed.set(courseLanding || url.includes('/learn/course'));
    });
  }
}
