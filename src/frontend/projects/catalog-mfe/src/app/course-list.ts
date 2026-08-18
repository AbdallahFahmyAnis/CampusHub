import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CatalogApi, CourseListItemDto, hoursLabel, starSlots } from './catalog.api';

@Component({
  selector: 'app-course-list',
  imports: [RouterLink, DecimalPipe],
  template: `
    <div class="udemy-catalog">
      <div class="page-head">
        <div>
          <h1>All courses</h1>
          <p class="page-kicker">Learn a topic from preview lectures, then enroll for the full curriculum, Q&amp;A, and a signed pass.</p>
        </div>
      </div>
      @if (error()) {
        <p class="error">{{ error() }}</p>
      } @else if (!loaded()) {
        <p class="muted">Loading catalog…</p>
      } @else if (courses().length === 0) {
        <div class="empty">
          <p class="empty-title">No published courses yet</p>
          <p class="muted">When a teacher publishes a course, it will appear here.</p>
        </div>
      } @else {
        <div class="udemy-grid">
          @for (course of courses(); track course.id) {
            <a class="udemy-card" [routerLink]="['/catalog', course.id]">
              <div class="udemy-cover" [attr.data-subject]="course.subjectCode">
                <span>{{ course.subjectCode }}</span>
              </div>
              <div class="udemy-card-body">
                <h2>{{ course.title }}</h2>
                <p class="muted clip">{{ course.subtitle || course.subjectName }}</p>
                <p class="instructor">{{ course.teacherName }}</p>
                <div class="rating-line">
                  <strong>{{ course.ratingAverage || 'New' }}</strong>
                  <span class="stars" aria-hidden="true">
                    @for (slot of starSlots(course.ratingAverage); track $index) {
                      <span [class.on]="slot.on">★</span>
                    }
                  </span>
                  <span class="muted">({{ course.ratingCount }})</span>
                </div>
                <div class="udemy-card-meta">
                  <span>{{ course.lectureCount }} lectures · {{ hoursLabel(course.durationMinutes) }}</span>
                  @if (course.level) {
                    <span class="pill">{{ course.level }}</span>
                  }
                </div>
                <p class="price">{{ course.price | number: '1.2-2' }} USD</p>
              </div>
            </a>
          }
        </div>
      }
    </div>
  `,
})
export class CourseList {
  private readonly api = inject(CatalogApi);
  readonly courses = signal<CourseListItemDto[]>([]);
  readonly error = signal<string | null>(null);
  readonly loaded = signal(false);
  readonly starSlots = starSlots;
  readonly hoursLabel = hoursLabel;

  constructor() {
    void this.api.courses()
      .then((items) => this.courses.set(items))
      .catch(() => this.error.set('Could not load the catalog.'))
      .finally(() => this.loaded.set(true));
  }
}
