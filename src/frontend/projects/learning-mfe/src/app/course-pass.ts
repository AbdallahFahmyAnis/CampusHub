import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CredentialDto, LearningApi } from './learning.api';

@Component({
  selector: 'app-course-pass',
  imports: [DatePipe, RouterLink],
  template: `
    <div class="page-head">
      <div>
        <h1>Course passes</h1>
        <p class="page-kicker">Signed QR credentials are issued after enrollment is confirmed. Show this pass at the door, or let a teacher scan the token.</p>
      </div>
    </div>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    <div class="cards">
      @for (item of items(); track item.id) {
        <article class="card pass-card">
          @if (item.status === 'Active') {
            <div class="qr-frame">
              <img class="qr" [src]="qrUrl(item.id)" [alt]="'QR pass for ' + item.courseTitle" />
            </div>
          }
          <div>
            <h2>{{ item.courseTitle }}</h2>
            <p>
              <span class="pill" [attr.data-status]="item.status">{{ item.status }}</span>
              <span class="pill">{{ item.kind }}</span>
            </p>
            @if (item.status === 'Active') {
              <p class="muted token">{{ item.token }}</p>
              <p class="muted">Expires {{ item.expiresAt | date: 'mediumDate' }}</p>
            }
          </div>
        </article>
      } @empty {
        <div class="empty">
          <p class="empty-title">No passes yet</p>
          <p class="muted"><a routerLink="/catalog">Enroll in a published course</a> to receive a signed QR pass.</p>
        </div>
      }
    </div>
  `,
})
export class CoursePass {
  private readonly api = inject(LearningApi);
  readonly items = signal<CredentialDto[]>([]);
  readonly error = signal<string | null>(null);

  constructor() {
    void this.api.credentials()
      .then((items) => this.items.set(items))
      .catch(() => this.error.set('Could not load course passes.'));
  }

  qrUrl(id: string): string {
    return `/api/access/credentials/${id}/qr`;
  }
}
