import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CatalogApi, CourseListItemDto, SubjectDto, hoursLabel, starSlots } from './catalog.api';

@Component({
  selector: 'app-course-list',
  imports: [RouterLink, DecimalPipe],
  template: `
    <div class="udemy-catalog">
      <div class="page-head">
        <div>
          <h1>All courses</h1>
          <p class="page-kicker">Filter by category, preview a lecture, then enroll for the full curriculum, Q&amp;A, and a signed pass.</p>
        </div>
        @if (totalCount()) {
          <p class="muted catalog-count">{{ totalCount() }} course{{ totalCount() === 1 ? '' : 's' }}</p>
        }
      </div>
      @if (subjects().length) {
        <div class="catalog-filters" role="tablist" aria-label="Categories">
          <button
            type="button"
            class="chip"
            [class.active]="!category()"
            (click)="setCategory('')"
          >All</button>
          @for (subject of subjects(); track subject.id) {
            <button
              type="button"
              class="chip"
              [class.active]="category() === subject.code"
              (click)="setCategory(subject.code)"
            >{{ subject.name }}</button>
          }
        </div>
      }
      @if (error()) {
        <p class="error">{{ error() }}</p>
      } @else if (!loaded()) {
        <p class="muted">Loading catalog…</p>
      } @else if (courses().length === 0) {
        <div class="empty">
          <p class="empty-title">{{ category() ? 'No courses in this category' : 'No published courses yet' }}</p>
          <p class="muted">{{ category() ? 'Try another category, or choose All.' : 'When a teacher publishes a course, it will appear here.' }}</p>
        </div>
      } @else {
        <div class="udemy-grid">
          @for (course of courses(); track course.id) {
            <a class="udemy-card" [routerLink]="['/catalog', course.id]">
              <div class="udemy-cover" [attr.data-subject]="course.subjectCode">
                <span>{{ course.subjectCode }}</span>
              </div>
              <div class="udemy-card-body">
                <p class="card-kicker">{{ course.subjectName }}</p>
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
        @if (totalPages() > 1) {
          <nav class="pager" aria-label="Catalog pages">
            <button type="button" class="btn secondary" [disabled]="page() <= 1" (click)="goPage(page() - 1)">Previous</button>
            <span class="pager-status">Page {{ page() }} of {{ totalPages() }}</span>
            <button type="button" class="btn secondary" [disabled]="page() >= totalPages()" (click)="goPage(page() + 1)">Next</button>
          </nav>
        }
      }
    </div>
  `,
})
export class CourseList {
  private readonly api = inject(CatalogApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly courses = signal<CourseListItemDto[]>([]);
  readonly subjects = signal<SubjectDto[]>([]);
  readonly error = signal<string | null>(null);
  readonly loaded = signal(false);
  readonly category = signal('');
  readonly page = signal(1);
  readonly pageSize = 12;
  readonly totalCount = signal(0);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  readonly starSlots = starSlots;
  readonly hoursLabel = hoursLabel;

  constructor() {
    void this.api.subjects()
      .then((items) => this.subjects.set(items))
      .catch(() => undefined);

    this.route.queryParamMap.subscribe((params) => {
      this.category.set((params.get('category') ?? '').toUpperCase());
      const nextPage = Number(params.get('page'));
      this.page.set(Number.isFinite(nextPage) && nextPage > 0 ? nextPage : 1);
      void this.load();
    });
  }

  setCategory(code: string): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { category: code || null, page: 1 },
      queryParamsHandling: 'merge',
    });
  }

  goPage(next: number): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: next },
      queryParamsHandling: 'merge',
    });
  }

  private async load(): Promise<void> {
    this.loaded.set(false);
    this.error.set(null);
    try {
      const result = await this.api.courses({
        category: this.category() || undefined,
        page: this.page(),
        pageSize: this.pageSize,
      });
      this.courses.set(result.items);
      this.totalCount.set(result.totalCount);
      if (this.page() > 1 && result.items.length === 0 && result.totalCount > 0) {
        this.goPage(1);
      }
    } catch {
      this.error.set('Could not load the catalog.');
    } finally {
      this.loaded.set(true);
    }
  }
}
