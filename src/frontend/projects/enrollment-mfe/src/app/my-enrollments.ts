import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EnrollmentApi, EnrollmentDto, WaitlistEntryDto } from './enrollment.api';

/** SDD CH-S19 enrollments + CH-S23 waitlist */
@Component({
  selector: 'app-my-enrollments',
  imports: [RouterLink, DecimalPipe],
  template: `
    <div class="page-head">
      <div>
        <h1>My enrollments</h1>
        <p class="page-kicker">Follow each saga from seat reservation through payment to confirmation.</p>
      </div>
    </div>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    } @else if (!loaded()) {
      <p class="muted">Loading enrollments…</p>
    } @else {
      <section class="panel" style="margin-bottom: 1.5rem;">
        <h2>Waitlist</h2>
        @if (waitlist().length === 0) {
          <p class="muted">You are not waiting for any full courses. When a course shows 0 seats, join from the course page.</p>
        } @else {
          <div class="cards catalog">
            @for (item of waitlist(); track item.id) {
              <a class="card" [routerLink]="['/catalog', item.courseId]">
                <h2>{{ item.courseTitle }}</h2>
                <div class="meta">
                  <span class="pill">Position {{ item.position }} of {{ item.queueLength }}</span>
                </div>
              </a>
            }
          </div>
        }
      </section>

      @if (items().length === 0) {
        <div class="empty">
          <p class="empty-title">No enrollments yet</p>
          <p class="muted"><a routerLink="/catalog">Open the catalog</a> and enroll in a published course.</p>
        </div>
      } @else {
        <div class="cards catalog">
          @for (item of items(); track item.id) {
            <a class="card" [routerLink]="item.status === 'Confirmed' ? ['/learn', 'course', item.courseId] : ['/enroll', item.courseId]">
              <h2>{{ item.courseTitle }}</h2>
              <div class="meta">
                <span class="pill" [attr.data-status]="item.status">{{ item.status }}</span>
                <span class="price">{{ item.amount | number: '1.2-2' }} USD</span>
              </div>
            </a>
          }
        </div>
      }
    }
  `,
})
export class MyEnrollments {
  private readonly api = inject(EnrollmentApi);
  readonly items = signal<EnrollmentDto[]>([]);
  readonly waitlist = signal<WaitlistEntryDto[]>([]);
  readonly error = signal<string | null>(null);
  readonly loaded = signal(false);

  constructor() {
    void Promise.all([this.api.mine(), this.api.waitlistMine().catch(() => [])])
      .then(([items, waitlist]) => {
        this.items.set(items);
        this.waitlist.set(waitlist);
      })
      .catch(() => this.error.set('Could not load enrollments.'))
      .finally(() => this.loaded.set(true));
  }
}
