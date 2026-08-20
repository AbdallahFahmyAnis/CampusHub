/** SDD CH-S24 — teacher enrollment roster. /catalog/:id/roster */
import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CatalogApi, CourseRosterDto } from './catalog.api';

@Component({
  selector: 'app-course-roster',
  imports: [RouterLink, DatePipe],
  template: `
    <a class="back-link" routerLink="/catalog/mine">Back to my courses</a>
    <div class="page-head">
      <div>
        <h1>{{ roster()?.courseTitle || 'Enrollment roster' }}</h1>
        <p class="page-kicker">Confirmed enrollments from the enrollment service — includes students before they submit work.</p>
      </div>
      @if (courseId(); as id) {
        <div class="actions" style="display:flex;gap:.5rem;flex-wrap:wrap;">
          <a class="btn secondary" [routerLink]="['/catalog', id, 'edit']">Edit course</a>
          <a class="btn secondary" [routerLink]="['/catalog', id, 'gradebook']">Gradebook</a>
          <a class="btn secondary" [routerLink]="['/catalog', id, 'analytics']">Analytics</a>
        </div>
      }
    </div>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    } @else if (!roster()) {
      <p class="muted">Loading roster…</p>
    } @else if (roster(); as r) {
      @if (r.students.length === 0) {
        <div class="empty">
          <p class="empty-title">No confirmed enrollments yet</p>
          <p class="muted">Students appear here after mock checkout completes. Gradebook only lists students who submitted quizzes or assignments.</p>
        </div>
      } @else {
        <p class="muted" style="margin-bottom:1rem;">{{ r.confirmedCount }} confirmed</p>
        <section class="panel" style="overflow:auto;">
          <table style="width:100%;border-collapse:collapse;min-width:420px;">
            <thead>
              <tr>
                <th style="text-align:left;padding:.6rem .4rem;">Student</th>
                <th style="text-align:left;padding:.6rem .4rem;">Email</th>
                <th style="text-align:left;padding:.6rem .4rem;">Enrolled</th>
              </tr>
            </thead>
            <tbody>
              @for (row of r.students; track row.studentId) {
                <tr style="border-top:1px solid var(--border,#e5e7eb);">
                  <td style="padding:.6rem .4rem;">{{ row.studentName }}</td>
                  <td style="padding:.6rem .4rem;">{{ row.studentEmail }}</td>
                  <td style="padding:.6rem .4rem;">{{ row.enrolledAt | date: 'mediumDate' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </section>
      }
    }
  `,
})
export class CourseRoster {
  private readonly api = inject(CatalogApi);
  private readonly route = inject(ActivatedRoute);
  readonly roster = signal<CourseRosterDto | null>(null);
  readonly error = signal<string | null>(null);
  readonly courseId = signal<string | null>(this.route.snapshot.paramMap.get('id'));

  constructor() {
    const id = this.courseId();
    if (!id) {
      this.error.set('Missing course id.');
      return;
    }
    void this.api.roster(id)
      .then((r) => this.roster.set(r))
      .catch(() => this.error.set('Could not load the roster. Confirm you own this course.'));
  }
}
