/** SDD CH-S06 / MDP-17 — completion certificates. /learn/certificates */
import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CredentialDto, LearningApi } from './learning.api';

@Component({
  selector: 'app-certificates',
  imports: [DatePipe, RouterLink],
  template: `
    <div class="page-head">
      <div>
        <h1>Certificates</h1>
        <p class="page-kicker">Completion certificates are issued automatically when you finish every lecture in a course.</p>
      </div>
    </div>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    <div class="cards">
      @for (item of items(); track item.id) {
        <article class="card pass-card">
          <div class="cert-badge" aria-hidden="true">🎓</div>
          <div>
            <h2>{{ item.courseTitle }}</h2>
            <p>
              <span class="pill" [attr.data-status]="item.status">{{ item.status }}</span>
              <span class="pill">{{ item.kind }}</span>
            </p>
            <p class="muted">Issued {{ item.issuedAt | date: 'mediumDate' }}</p>
            @if (item.status === 'Active') {
              <p class="actions" style="display:flex; gap: .5rem; flex-wrap: wrap; margin-top: .75rem;">
                <a class="btn" [href]="qrUrl(item.id)" target="_blank" rel="noopener">View QR</a>
                <a class="btn secondary" [href]="qrUrl(item.id)" [download]="item.courseTitle + ' Certificate.png'">Download</a>
              </p>
              <p class="muted token" style="margin-top:.5rem; font-size:.75rem; word-break:break-all;">{{ item.token }}</p>
            }
          </div>
        </article>
      } @empty {
        <div class="empty">
          <p class="empty-title">No certificates yet</p>
          <p class="muted">Complete every lecture in an enrolled course to earn a certificate.</p>
          <a class="btn" routerLink="/catalog">Browse courses</a>
        </div>
      }
    </div>
  `,
  styles: [`
    .cert-badge { font-size: 2.5rem; margin-bottom: .5rem; }
  `]
})
export class Certificates {
  private readonly api = inject(LearningApi);
  readonly items = signal<CredentialDto[]>([]);
  readonly error = signal<string | null>(null);

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    try {
      const all = await this.api.credentials();
      this.items.set(all.filter((c) => c.kind === 'Certificate'));
    } catch {
      this.error.set('Could not load certificates.');
    }
  }

  qrUrl(id: string): string {
    return `/api/access/credentials/${id}/qr`;
  }
}
