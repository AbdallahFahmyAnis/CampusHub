import { Component, HostListener, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import {
  NotificationAlerts,
  NotificationDto,
  noticeGlyph,
  noticeKind,
  timeAgo,
} from './notifications';
import { SessionService } from './session';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  readonly session = inject(SessionService);
  readonly alerts = inject(NotificationAlerts);
  private readonly router = inject(Router);
  readonly menuOpen = signal(false);
  readonly bleed = signal(false);
  readonly alertsOpen = signal(false);
  readonly alertTab = signal<'all' | 'unread'>('all');
  searchQuery = '';
  readonly timeAgo = timeAgo;
  readonly visibleAlerts = computed(() => {
    const items = this.alerts.all();
    const filtered = this.alertTab() === 'unread' ? items.filter((item) => !item.read) : items;
    return filtered.slice(0, 12);
  });

  constructor() {
    void this.session.load().then((session) => {
      if (session.authenticated) {
        void this.alerts.refresh();
        this.alerts.startSse();
      }
    });
    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe((event) => {
      this.menuOpen.set(false);
      this.alertsOpen.set(false);
      const url = event.urlAfterRedirects.split('?')[0];
      const courseLanding = /\/catalog\/[0-9a-fA-F-]{36}$/.test(url);
      this.bleed.set(courseLanding || url.includes('/learn/course'));
    });
    setInterval(() => {
      if (this.session.session().authenticated) {
        void this.alerts.refresh();
      }
    }, 30000);
  }

  @HostListener('document:click')
  closeAlerts(): void {
    this.alertsOpen.set(false);
  }

  toggleAlerts(event: Event): void {
    event.stopPropagation();
    const next = !this.alertsOpen();
    this.alertsOpen.set(next);
    if (next) {
      this.alertTab.set('all');
      void this.alerts.refresh();
    }
  }

  async markAllRead(event: Event): Promise<void> {
    event.stopPropagation();
    await this.alerts.markAllRead();
  }

  async openAlert(item: NotificationDto): Promise<void> {
    if (!item.read) {
      await this.alerts.markRead(item.id);
    }
    this.alertsOpen.set(false);
    await this.router.navigateByUrl('/learn/inbox');
  }

  kindOf(item: NotificationDto): string {
    return noticeKind(item.eventType);
  }

  glyphOf(item: NotificationDto): string {
    return noticeGlyph(noticeKind(item.eventType));
  }

  searchCatalog(event: Event): void {
    event.preventDefault();
    const q = this.searchQuery.trim();
    void this.router.navigate(['/catalog'], { queryParams: q ? { q } : {} });
  }
}
