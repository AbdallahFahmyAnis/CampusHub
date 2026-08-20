/** SDD CH-S07 / MDP-18 — teacher analytics. /catalog/:id/analytics */
import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CatalogApi, CourseStatsDto } from './catalog.api';

@Component({
  selector: 'app-course-analytics',
  imports: [RouterLink, DecimalPipe],
  template: `
    <div class="page-head">
      <div>
        <h1>{{ stats()?.courseTitle || 'Course analytics' }}</h1>
        <p class="page-kicker">Enrollment, revenue, and completion data for this course.</p>
      </div>
      @if (courseId()) {
        <div class="actions" style="display:flex;gap:.5rem;flex-wrap:wrap;">
          <a class="btn secondary" [routerLink]="['/catalog', courseId(), 'edit']">Edit course</a>
          <a class="btn secondary" [routerLink]="['/catalog', courseId(), 'gradebook']">Gradebook</a>
          <a class="btn secondary" [routerLink]="['/catalog', courseId(), 'roster']">Roster</a>
        </div>
      }
    </div>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    @if (stats(); as s) {
      <!-- Top-line stats -->
      <div class="catalog-filters" style="margin-bottom: 2rem; display: flex; flex-wrap: wrap; gap: 1rem;">
        <div class="profile-panel" style="flex: 1; min-width: 140px; padding: 1rem 1.5rem;">
          <p class="card-kicker">Enrollments</p>
          <p style="font-weight: 700; font-size: 1.4rem;">{{ s.confirmedEnrollments }}</p>
          <p class="muted">{{ s.totalEnrollments }} total · {{ s.cancelledEnrollments }} cancelled</p>
        </div>
        <div class="profile-panel" style="flex: 1; min-width: 140px; padding: 1rem 1.5rem;">
          <p class="card-kicker">Revenue</p>
          <p style="font-weight: 700; font-size: 1.4rem;">{{ s.totalRevenue | number:'1.0-0' }} USD</p>
          <p class="muted">from confirmed enrollments</p>
        </div>
        <div class="profile-panel" style="flex: 1; min-width: 140px; padding: 1rem 1.5rem;">
          <p class="card-kicker">Completions</p>
          <p style="font-weight: 700; font-size: 1.4rem;">{{ s.studentsCompletedAll }}</p>
          <p class="muted">
            {{ s.confirmedEnrollments > 0
              ? ((s.studentsCompletedAll / s.confirmedEnrollments) * 100 | number:'1.0-0') + '%'
              : '—' }} completion rate
          </p>
        </div>
        <div class="profile-panel" style="flex: 1; min-width: 140px; padding: 1rem 1.5rem;">
          <p class="card-kicker">Rating</p>
          <p style="font-weight: 700; font-size: 1.4rem;">{{ s.averageRating | number:'1.1-1' }} ★</p>
          <p class="muted">{{ s.reviewCount }} review{{ s.reviewCount === 1 ? '' : 's' }}</p>
        </div>
        <div class="profile-panel" style="flex: 1; min-width: 140px; padding: 1rem 1.5rem;">
          <p class="card-kicker">Lectures</p>
          <p style="font-weight: 700; font-size: 1.4rem;">{{ s.totalLectures }}</p>
          <p class="muted">in this course</p>
        </div>
      </div>

      <!-- Monthly breakdown -->
      @if (s.monthlyBreakdown.length) {
        <section class="panel" style="margin-bottom: 2rem;">
          <h2>Monthly enrollments (last 6 months)</h2>
          <table style="width: 100%; border-collapse: collapse;">
            <thead>
              <tr>
                <th style="text-align: left; padding: .5rem 0;">Month</th>
                <th style="text-align: right; padding: .5rem 0;">Enrollments</th>
                <th style="text-align: right; padding: .5rem 0;">Revenue (USD)</th>
              </tr>
            </thead>
            <tbody>
              @for (m of s.monthlyBreakdown; track m.month) {
                <tr style="border-top: 1px solid var(--border, #e5e7eb);">
                  <td style="padding: .5rem 0;">{{ m.month }}</td>
                  <td style="text-align: right; padding: .5rem 0;">{{ m.count }}</td>
                  <td style="text-align: right; padding: .5rem 0;">{{ m.revenue | number:'1.0-0' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </section>
      }

      <!-- Lecture completion breakdown -->
      @if (s.lectureStats.length) {
        <section class="panel">
          <h2>Lecture completions</h2>
          <p class="muted" style="margin-bottom: 1rem;">Ordered by how many students have completed each lecture.</p>
          @for (l of s.lectureStats; track l.id) {
            <div style="display: flex; align-items: center; gap: 1rem; padding: .4rem 0; border-bottom: 1px solid var(--border, #e5e7eb);">
              <div style="flex: 1;">
                <p style="margin: 0; font-weight: 500;">{{ l.title }}</p>
                <p class="muted" style="margin: 0; font-size: .85rem;">{{ l.sectionTitle }} · {{ l.durationMinutes }}m</p>
              </div>
              <div style="text-align: right; min-width: 80px;">
                <p style="margin: 0; font-weight: 700;">{{ l.completionCount }}</p>
                <p class="muted" style="margin: 0; font-size: .8rem;">completed</p>
              </div>
              <div style="width: 120px;">
                <div style="background: var(--border, #e5e7eb); border-radius: 4px; height: 8px;">
                  <div style="background: var(--accent, #4f46e5); border-radius: 4px; height: 8px;"
                    [style.width.%]="s.confirmedEnrollments > 0 ? (l.completionCount / s.confirmedEnrollments) * 100 : 0">
                  </div>
                </div>
              </div>
            </div>
          }
        </section>
      }
    }
  `,
})
export class CourseAnalytics {
  private readonly api = inject(CatalogApi);
  private readonly route = inject(ActivatedRoute);
  readonly stats = signal<CourseStatsDto | null>(null);
  readonly error = signal<string | null>(null);
  readonly courseId = signal<string | null>(null);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    this.courseId.set(id);
    if (id) {
      void this.api.courseStats(id)
        .then((s) => this.stats.set(s))
        .catch(() => this.error.set('Could not load analytics for this course.'));
    }
  }
}
