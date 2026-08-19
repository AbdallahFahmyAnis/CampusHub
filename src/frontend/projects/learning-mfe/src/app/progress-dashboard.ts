import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LearningApi, ProgressDashboardDto } from './learning.api';

@Component({
  selector: 'app-progress-dashboard',
  imports: [RouterLink, DatePipe],
  template: `
    <div class="page-head">
      <div>
        <h1>My learning</h1>
        <p class="page-kicker">Track your progress, pick up where you left off, and keep your streak going.</p>
      </div>
      <a class="btn secondary" routerLink="/catalog">Browse courses</a>
    </div>

    @if (error()) {
      <p class="error">{{ error() }}</p>
    }

    @if (data(); as d) {
      <!-- Stats bar -->
      <div style="display:flex;flex-wrap:wrap;gap:1rem;margin-bottom:2rem;">
        <div class="profile-panel" style="flex:1;min-width:140px;padding:1rem 1.5rem;">
          <p class="card-kicker">Day streak</p>
          <p style="font-weight:700;font-size:1.6rem;">{{ d.streakDays }} 🔥</p>
          <p class="muted">Keep it up — complete a lecture today!</p>
        </div>
        <div class="profile-panel" style="flex:1;min-width:140px;padding:1rem 1.5rem;">
          <p class="card-kicker">Lectures completed</p>
          <p style="font-weight:700;font-size:1.6rem;">{{ d.totalLecturesCompleted }}</p>
          <p class="muted">across {{ d.courses.length }} course{{ d.courses.length === 1 ? '' : 's' }}</p>
        </div>
        @if (d.lastActivityAt) {
          <div class="profile-panel" style="flex:1;min-width:140px;padding:1rem 1.5rem;">
            <p class="card-kicker">Last activity</p>
            <p style="font-weight:700;font-size:1rem;">{{ d.lastActivityAt | date:'mediumDate' }}</p>
            <p class="muted">{{ d.lastActivityAt | date:'shortTime' }}</p>
          </div>
        }
        <div class="profile-panel" style="flex:1;min-width:140px;padding:1rem 1.5rem;">
          <p class="card-kicker">Quick links</p>
          <p style="margin:.2rem 0;"><a routerLink="/learn/certificates" style="font-size:.9rem;">Certificates</a></p>
          <p style="margin:.2rem 0;"><a routerLink="/learn/inbox" style="font-size:.9rem;">Notifications</a></p>
          <p style="margin:.2rem 0;"><a routerLink="/catalog" [queryParams]="{saved:'1'}" style="font-size:.9rem;">Wishlist</a></p>
        </div>
      </div>

      <!-- Continue learning (first in-progress course) -->
      @if (continueItem(d); as c) {
        <section class="panel" style="margin-bottom:2rem;background:var(--accent-soft,#eef2ff);border:1px solid var(--accent,#4f46e5);">
          <p class="card-kicker" style="color:var(--accent,#4f46e5);">Continue where you left off</p>
          <div style="display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:1rem;">
            <div>
              <h2 style="margin:0;">{{ c.courseTitle }}</h2>
              <p class="muted" style="margin:.25rem 0 0;">{{ c.completedLectures }} / {{ c.totalLectures }} lectures · {{ c.progressPct }}% complete</p>
            </div>
            @if (c.continueLectureId) {
              <a class="btn" [routerLink]="['/learn/course', c.courseId, c.continueLectureId]">Continue</a>
            } @else {
              <a class="btn" [routerLink]="['/learn/course', c.courseId]">Open course</a>
            }
          </div>
          <div style="margin-top:.75rem;background:var(--border,#e5e7eb);border-radius:4px;height:8px;">
            <div style="background:var(--accent,#4f46e5);border-radius:4px;height:8px;transition:width .3s;"
              [style.width.%]="c.progressPct"></div>
          </div>
        </section>
      }

      <!-- All enrolled courses -->
      @if (d.courses.length === 0) {
        <div class="empty">
          <p class="empty-title">No courses yet</p>
          <p class="muted">Enroll in a course to start tracking your progress here.</p>
          <a class="btn" routerLink="/catalog">Browse catalog</a>
        </div>
      } @else {
        <section>
          <h2 style="margin-bottom:1rem;">All enrolled courses</h2>
          <div style="display:flex;flex-direction:column;gap:1rem;">
            @for (course of d.courses; track course.courseId) {
              <div class="panel" style="display:flex;align-items:center;gap:1rem;flex-wrap:wrap;">
                <div class="udemy-cover" [attr.data-subject]="course.subjectCode"
                  style="width:56px;height:56px;border-radius:8px;flex-shrink:0;display:flex;align-items:center;justify-content:center;font-weight:700;">
                  <span>{{ course.subjectCode }}</span>
                </div>
                <div style="flex:1;min-width:180px;">
                  <p style="margin:0;font-weight:600;">{{ course.courseTitle }}</p>
                  <p class="muted" style="margin:.2rem 0 .5rem;font-size:.85rem;">
                    {{ course.completedLectures }} / {{ course.totalLectures }} lectures
                    @if (course.lastActivityAt) {
                      · Last: {{ course.lastActivityAt | date:'mediumDate' }}
                    }
                  </p>
                  <div style="background:var(--border,#e5e7eb);border-radius:4px;height:6px;">
                    <div [style.width.%]="course.progressPct"
                      [style.background]="course.progressPct === 100 ? '#16a34a' : 'var(--accent,#4f46e5)'"
                      style="border-radius:4px;height:6px;transition:width .3s;"></div>
                  </div>
                </div>
                <div style="text-align:right;flex-shrink:0;">
                  <p style="margin:0;font-weight:700;font-size:1.1rem;">{{ course.progressPct }}%</p>
                  @if (course.progressPct === 100) {
                    <span class="pill" data-status="Published" style="font-size:.75rem;">Complete ✓</span>
                  }
                </div>
                <div style="flex-shrink:0;">
                  @if (course.continueLectureId && course.progressPct < 100) {
                    <a class="btn secondary" [routerLink]="['/learn/course', course.courseId, course.continueLectureId]">Continue</a>
                  } @else {
                    <a class="btn secondary" [routerLink]="['/learn/course', course.courseId]">Open</a>
                  }
                </div>
              </div>
            }
          </div>
        </section>
      }
    } @else if (!error()) {
      <p class="muted">Loading your progress…</p>
    }
  `,
})
export class ProgressDashboard {
  private readonly api = inject(LearningApi);
  readonly data = signal<ProgressDashboardDto | null>(null);
  readonly error = signal<string | null>(null);

  constructor() {
    void this.api.progressDashboard()
      .then((d) => this.data.set(d))
      .catch(() => this.error.set('Could not load your progress. Make sure you are signed in.'));
  }

  continueItem(d: ProgressDashboardDto) {
    return d.courses.find((c) => c.progressPct > 0 && c.progressPct < 100) ?? d.courses[0] ?? null;
  }
}
