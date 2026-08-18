import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { LearningApi, NotificationDto } from './learning.api';

@Component({
  selector: 'app-inbox',
  imports: [DatePipe],
  template: `
    <div class="page-head">
      <div>
        <h1>Inbox</h1>
        <p class="page-kicker">Enrollment, payment, and access messages delivered after the saga completes.</p>
      </div>
    </div>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    <div class="cards">
      @for (item of items(); track item.id) {
        <article class="card" [class.unread]="!item.read">
          <div class="card-head">
            <h2>{{ item.title }}</h2>
            @if (!item.read) {
              <span class="pill flag">Unread</span>
            }
          </div>
          <p>{{ item.body }}</p>
          <p class="muted">{{ item.createdAt | date: 'medium' }}</p>
          @if (!item.read) {
            <div class="actions">
              <button class="btn secondary" type="button" (click)="read(item)">Mark read</button>
            </div>
          }
        </article>
      } @empty {
        <div class="empty">
          <p class="empty-title">Inbox is quiet</p>
          <p class="muted">Enroll in a course to see confirmation and payment messages.</p>
        </div>
      }
    </div>
  `,
})
export class Inbox {
  private readonly api = inject(LearningApi);
  readonly items = signal<NotificationDto[]>([]);
  readonly error = signal<string | null>(null);

  constructor() {
    void this.refresh();
  }

  async read(item: NotificationDto): Promise<void> {
    await this.api.markRead(item.id);
    await this.refresh();
  }

  private async refresh(): Promise<void> {
    try {
      this.items.set(await this.api.mine());
    } catch {
      this.error.set('Could not load notifications.');
    }
  }
}
