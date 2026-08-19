/** SDD CH-S19 / specs/019-enroll-checkout — mock pay checkout. /enroll/:courseId */
import { DecimalPipe } from '@angular/common';
import { Component, OnDestroy, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EnrollmentApi, EnrollmentDto } from './enrollment.api';

@Component({
  selector: 'app-checkout',
  imports: [RouterLink, DecimalPipe],
  template: `
    <a class="back-link" routerLink="/catalog">Back to catalog</a>
    <article class="hero-card">
      <h1>Enroll</h1>
      <p class="lede">This starts the enrollment saga: reserve a seat, charge the mock payment provider, then confirm or compensate.</p>

      @if (!enrollment()) {
        <div class="choice-grid">
          <button class="choice" type="button" [disabled]="busy()" (click)="start('Succeeded')">
            <strong>Pay successfully</strong>
            <span class="muted">Happy path — seat reserved, payment captured, enrollment confirmed.</span>
          </button>
          <button class="choice danger" type="button" [disabled]="busy()" (click)="start('Failed')">
            <strong>Simulate payment failure</strong>
            <span class="muted">The saga compensates and releases the reserved seat.</span>
          </button>
        </div>
      }

      @if (error()) {
        <p class="error">{{ error() }}</p>
      }

      @if (enrollment(); as item) {
        <div class="card nested">
          <h2>{{ item.courseTitle }}</h2>
          <div class="meta">
            <span class="pill" [attr.data-status]="item.status">{{ item.status }}</span>
            <span class="price">{{ item.amount | number: '1.2-2' }} USD</span>
          </div>
          @if (item.failureReason) {
            <p class="error">{{ item.failureReason }}</p>
          }
          @if (item.status === 'PaymentPending' || item.status === 'SeatReserved') {
            <p class="muted">Waiting on the mock payment provider…</p>
          }
          @if (item.status === 'Confirmed') {
            <p class="success">Enrollment confirmed. The seat is yours.</p>
            <div class="actions">
              <a class="btn" [routerLink]="['/learn', 'course', item.courseId]">Go to course</a>
              <a class="btn secondary" routerLink="/learn">Course pass</a>
            </div>
          }
          @if (item.status === 'Compensated' || item.status === 'Rejected') {
            <p>The saga compensated. The reserved seat was released if one was held.</p>
          }
        </div>
      }
    </article>
  `,
})
export class Checkout implements OnDestroy {
  private readonly api = inject(EnrollmentApi);
  private readonly route = inject(ActivatedRoute);
  readonly enrollment = signal<EnrollmentDto | null>(null);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  private timer: ReturnType<typeof setInterval> | undefined;

  async start(simulatePayment: 'Succeeded' | 'Failed'): Promise<void> {
    const courseId = this.route.snapshot.paramMap.get('courseId');
    if (!courseId) {
      this.error.set('Missing course id.');
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    try {
      const created = await this.api.start(courseId, simulatePayment);
      this.enrollment.set(created);
      this.poll(created.id);
    } catch {
      this.error.set('Could not start enrollment. The course may be full.');
    } finally {
      this.busy.set(false);
    }
  }

  private poll(id: string): void {
    this.clearTimer();
    this.timer = setInterval(() => {
      void this.api.get(id).then((item) => {
        this.enrollment.set(item);
        if (['Confirmed', 'Compensated', 'Rejected'].includes(item.status)) {
          this.clearTimer();
        }
      });
    }, 700);
  }

  ngOnDestroy(): void {
    this.clearTimer();
  }

  private clearTimer(): void {
    if (this.timer) {
      clearInterval(this.timer);
    }
  }
}
