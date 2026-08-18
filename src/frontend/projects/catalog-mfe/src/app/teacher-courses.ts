import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CatalogApi, CourseListItemDto } from './catalog.api';

@Component({
  selector: 'app-teacher-courses',
  imports: [RouterLink],
  template: `
    <div class="page-head">
      <div>
        <h1>My courses</h1>
        <p class="page-kicker">Draft, publish, and archive the courses you own.</p>
      </div>
      <a class="btn" routerLink="/catalog/new">Create course</a>
    </div>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    } @else if (!loaded()) {
      <p class="muted">Loading your courses…</p>
    } @else if (courses().length === 0) {
      <div class="empty">
        <p class="empty-title">No courses yet</p>
        <p class="muted">Create a draft, then publish it for students to enroll.</p>
      </div>
    } @else {
      <div class="cards catalog">
        @for (course of courses(); track course.id) {
          <a class="card" [routerLink]="['/catalog', course.id, 'edit']">
            <div class="card-kicker">{{ course.subjectCode }}</div>
            <h2>{{ course.title }}</h2>
            <div class="meta">
              <span class="pill" [attr.data-status]="course.status">{{ course.status }}</span>
              <span>{{ course.remainingSeats }} of {{ course.capacity }} seats left</span>
            </div>
          </a>
        }
      </div>
    }
  `,
})
export class TeacherCourses {
  private readonly api = inject(CatalogApi);
  readonly courses = signal<CourseListItemDto[]>([]);
  readonly error = signal<string | null>(null);
  readonly loaded = signal(false);

  constructor() {
    void this.api.mine()
      .then((items) => this.courses.set(items))
      .catch(() => this.error.set('Could not load your courses.'))
      .finally(() => this.loaded.set(true));
  }
}
