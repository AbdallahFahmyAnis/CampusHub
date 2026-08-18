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
          <h1>{{ heading() }}</h1>
          <p class="page-kicker">{{ kicker() }}</p>
          @if (searchEngine() === 'meilisearch' && query()) {
            <p class="muted">Ranked by Meilisearch.</p>
          }
        </div>
        @if (totalCount()) {
          <p class="muted catalog-count">{{ totalCount() }} course{{ totalCount() === 1 ? '' : 's' }}</p>
        }
      </div>
      @if (!saved() && subjects().length) {
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
          <p class="empty-title">{{ emptyTitle() }}</p>
          <p class="muted">{{ emptyBody() }}</p>
        </div>
      } @else {
        <div class="udemy-grid">
          @for (course of courses(); track course.id) {
            <article class="udemy-card">
              <button
                type="button"
                class="wish-btn"
                [class.on]="course.wishlisted"
                (click)="toggleWish($event, course)"
                [attr.aria-label]="course.wishlisted ? 'Remove from wishlist' : 'Add to wishlist'"
              >{{ course.wishlisted ? '♥' : '♡' }}</button>
              <a [routerLink]="['/catalog', course.id]">
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
            </article>
          }
        </div>
        @if (!saved() && totalPages() > 1) {
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
  readonly query = signal('');
  readonly saved = signal(false);
  readonly page = signal(1);
  readonly pageSize = 12;
  readonly totalCount = signal(0);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  readonly starSlots = starSlots;
  readonly hoursLabel = hoursLabel;
  readonly searchEngine = signal('sql');
  readonly heading = computed(() => {
    if (this.saved()) {
      return 'Wishlist';
    }
    if (this.query()) {
      return `Results for “${this.query()}”`;
    }
    return 'All courses';
  });
  readonly kicker = computed(() => {
    if (this.saved()) {
      return 'Courses you saved for later. Open one to preview a lecture or enroll.';
    }
    return 'Filter by category, preview a lecture, then enroll for the full curriculum, Q&A, and a signed pass.';
  });

  constructor() {
    void this.api.subjects()
      .then((items) => this.subjects.set(items))
      .catch(() => undefined);
    void this.api.capabilities()
      .then((caps) => this.searchEngine.set(caps.search))
      .catch(() => undefined);

    this.route.queryParamMap.subscribe((params) => {
      this.category.set((params.get('category') ?? '').toUpperCase());
      this.query.set((params.get('q') ?? '').trim());
      this.saved.set(params.get('saved') === '1');
      const nextPage = Number(params.get('page'));
      this.page.set(Number.isFinite(nextPage) && nextPage > 0 ? nextPage : 1);
      void this.load();
    });
  }

  setCategory(code: string): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { category: code || null, page: 1, saved: null },
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

  emptyTitle(): string {
    if (this.saved()) {
      return 'Your wishlist is empty';
    }
    if (this.query()) {
      return 'No courses matched that search';
    }
    return this.category() ? 'No courses in this category' : 'No published courses yet';
  }

  emptyBody(): string {
    if (this.saved()) {
      return 'Tap the heart on a course card to save it here.';
    }
    if (this.query()) {
      return 'Try another keyword, or clear the search and browse by category.';
    }
    return this.category() ? 'Try another category, or choose All.' : 'When a teacher publishes a course, it will appear here.';
  }

  async toggleWish(event: Event, course: CourseListItemDto): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    const next = !course.wishlisted;
    try {
      if (next) {
        await this.api.addWishlist(course.id);
      } else {
        await this.api.removeWishlist(course.id);
      }
      this.courses.update((items) =>
        this.saved() && !next
          ? items.filter((item) => item.id !== course.id)
          : items.map((item) => (item.id === course.id ? { ...item, wishlisted: next } : item)),
      );
      if (this.saved() && !next) {
        this.totalCount.update((count) => Math.max(0, count - 1));
      }
    } catch {
      this.error.set('Could not update your wishlist.');
    }
  }

  private async load(): Promise<void> {
    this.loaded.set(false);
    this.error.set(null);
    try {
      if (this.saved()) {
        const items = await this.api.wishlist();
        this.courses.set(items);
        this.totalCount.set(items.length);
      } else {
        const result = await this.api.courses({
          category: this.category() || undefined,
          q: this.query() || undefined,
          page: this.page(),
          pageSize: this.pageSize,
        });
        this.courses.set(result.items);
        this.totalCount.set(result.totalCount);
        if (this.page() > 1 && result.items.length === 0 && result.totalCount > 0) {
          this.goPage(1);
        }
      }
    } catch {
      this.error.set('Could not load the catalog.');
    } finally {
      this.loaded.set(true);
    }
  }
}
