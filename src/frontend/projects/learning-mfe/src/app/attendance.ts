import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LearningApi, ScanDto } from './learning.api';

@Component({
  selector: 'app-attendance',
  imports: [DatePipe, FormsModule],
  template: `
    <div class="page-head">
      <div>
        <h1>Attendance</h1>
        <p class="page-kicker">Paste a student's signed pass token (or the text under their QR) to record a scan.</p>
      </div>
    </div>
    <form class="form" (submit)="submit($event)">
      <label>
        Pass token
        <textarea name="token" rows="3" [(ngModel)]="token" placeholder="Paste the signed token"></textarea>
      </label>
      <div class="actions">
        <button class="btn" type="submit" [disabled]="busy()">Record scan</button>
      </div>
    </form>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    @if (last(); as scan) {
      <p class="success">Recorded {{ scan.studentName }} for {{ scan.courseTitle }}.</p>
    }
    <h2 class="section-title">Recent scans</h2>
    <div class="cards">
      @for (item of items(); track item.id) {
        <article class="card">
          <h2>{{ item.studentName }}</h2>
          <p>{{ item.courseTitle }}</p>
          <p class="muted">{{ item.scannedAt | date: 'medium' }} · {{ item.scannedBy }}</p>
        </article>
      } @empty {
        <div class="empty">
          <p class="empty-title">No scans yet</p>
          <p class="muted">A recorded scan will appear here immediately.</p>
        </div>
      }
    </div>
  `,
})
export class Attendance {
  private readonly api = inject(LearningApi);
  token = '';
  readonly items = signal<ScanDto[]>([]);
  readonly last = signal<ScanDto | null>(null);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);

  constructor() {
    void this.refresh();
  }

  async submit(event: Event): Promise<void> {
    event.preventDefault();
    this.busy.set(true);
    this.error.set(null);
    try {
      const scan = await this.api.scan(this.token.trim());
      this.last.set(scan);
      this.token = '';
      await this.refresh();
    } catch {
      this.error.set('Scan failed. The pass may be invalid, expired, or revoked.');
    } finally {
      this.busy.set(false);
    }
  }

  private async refresh(): Promise<void> {
    try {
      this.items.set(await this.api.scans());
    } catch {
      this.error.set('Could not load attendance.');
    }
  }
}
