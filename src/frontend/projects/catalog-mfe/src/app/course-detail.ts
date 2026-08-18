import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  CatalogApi,
  CourseDetailDto,
  CurriculumDto,
  QuestionDto,
  ReviewDto,
  hoursLabel,
  starSlots,
} from './catalog.api';
import { SessionService } from '../../../shell/src/app/session';

@Component({
  selector: 'app-course-detail',
  imports: [RouterLink, DecimalPipe, DatePipe, FormsModule],
  template: `
    @if (course(); as item) {
      <div class="udemy-page">
        <section class="udemy-hero">
          <div class="udemy-wrap hero-grid">
            <div>
              <p class="breadcrumb">
                <a routerLink="/catalog">Catalog</a>
                <span> / {{ item.subjectName }}</span>
              </p>
              <h1>{{ item.title }}</h1>
              <p class="hero-sub">{{ item.subtitle || item.description }}</p>
              <div class="hero-meta">
                @if (item.ratingCount) {
                  <span class="rating-line light">
                    <strong>{{ item.ratingAverage }}</strong>
                    <span class="stars">
                      @for (slot of starSlots(item.ratingAverage); track $index) {
                        <span [class.on]="slot.on">★</span>
                      }
                    </span>
                    <span>({{ item.ratingCount }} ratings)</span>
                  </span>
                }
                <span>{{ item.lectureCount }} lectures</span>
                <span>{{ hoursLabel(item.durationMinutes) }} total</span>
                @if (item.level) {
                  <span>{{ item.level }}</span>
                }
              </div>
              <p>Created by <strong>{{ item.teacherName }}</strong></p>
            </div>
            <aside class="buy-card">
              <div class="udemy-cover buy-cover" [attr.data-subject]="item.subjectCode">
                <span>{{ item.subjectCode }}</span>
              </div>
              <p class="buy-price">{{ item.price | number: '1.2-2' }} USD</p>
              @if (item.enrolled) {
                <a class="btn buy" [routerLink]="['/learn', 'course', item.id]">Go to course</a>
              } @else if (!session.isTeacher() && item.canEnroll) {
                <a class="btn buy" [routerLink]="['/enroll', item.id]">Enroll now</a>
              } @else if (!item.canEnroll) {
                <p class="muted">This course is not open for enrollment.</p>
              }
              @if (session.isTeacher()) {
                <a class="btn secondary" [routerLink]="['/catalog', item.id, 'edit']">Edit course</a>
              }
              <ul class="buy-includes">
                <li>{{ item.lectureCount }} lectures on demand</li>
                <li>Q&amp;A with the instructor</li>
                <li>Student reviews</li>
                <li>Signed course pass after enrollment</li>
              </ul>
            </aside>
          </div>
        </section>

        <div class="udemy-wrap udemy-body">
          @if (item.outcomes.length) {
            <section class="panel">
              <h2>What you'll learn</h2>
              <ul class="outcomes">
                @for (line of item.outcomes; track line) {
                  <li>{{ line }}</li>
                }
              </ul>
            </section>
          }

          <section class="panel">
            <h2>Course content</h2>
            <p class="muted">{{ item.lectureCount }} lectures · {{ hoursLabel(item.durationMinutes) }}</p>
            @for (section of curriculum()?.sections ?? []; track section.id) {
              <details class="curr-section" open>
                <summary>{{ section.title }} <span class="muted">{{ section.lectures.length }} lectures</span></summary>
                <ul>
                  @for (lecture of section.lectures; track lecture.id) {
                    <li>
                      <span>{{ lecture.kind === 'Video' ? '▶' : '☰' }} {{ lecture.title }}</span>
                      <span class="muted">
                        @if (lecture.isPreview) {
                          <a [routerLink]="['/learn', 'course', item.id, lecture.id]">Preview</a>
                        }
                        {{ lecture.durationMinutes }}m
                      </span>
                    </li>
                  }
                </ul>
              </details>
            }
          </section>

          @if (item.requirements.length) {
            <section class="panel">
              <h2>Requirements</h2>
              <ul>
                @for (line of item.requirements; track line) {
                  <li>{{ line }}</li>
                }
              </ul>
            </section>
          }

          <section class="panel">
            <h2>Description</h2>
            <p>{{ item.description }}</p>
          </section>

          <section class="panel" id="reviews">
            <h2>Student reviews</h2>
            <div class="rating-line">
              <strong class="rating-xl">{{ item.ratingAverage || '—' }}</strong>
              <span class="stars">
                @for (slot of starSlots(item.ratingAverage); track $index) {
                  <span [class.on]="slot.on">★</span>
                }
              </span>
              <span class="muted">{{ item.ratingCount }} ratings</span>
            </div>
            @if (item.enrolled) {
              <form class="form stacked" (submit)="submitReview($event)">
                <p class="muted">Leave a rating and review. You can update it later.</p>
                <div class="star-input" role="radiogroup" aria-label="Rating">
                  @for (n of [1, 2, 3, 4, 5]; track n) {
                    <button type="button" class="star-btn" [class.on]="n <= reviewRating" (click)="reviewRating = n">★</button>
                  }
                </div>
                <label>Headline <input name="title" [(ngModel)]="reviewTitle" /></label>
                <label>Review <textarea name="body" rows="4" [(ngModel)]="reviewBody" required></textarea></label>
                <button class="btn" type="submit" [disabled]="busy()">Submit review</button>
              </form>
            } @else {
              <p class="muted">Enroll to leave a review.</p>
            }
            @for (review of reviews(); track review.id) {
              <article class="review">
                <div class="avatar">{{ initials(review.studentName) }}</div>
                <div>
                  <strong>{{ review.studentName }}</strong>
                  <div class="stars">
                    @for (slot of starSlots(review.rating); track $index) {
                      <span [class.on]="slot.on">★</span>
                    }
                  </div>
                  @if (review.title) {
                    <h3>{{ review.title }}</h3>
                  }
                  <p>{{ review.body }}</p>
                  <p class="muted">{{ review.createdAt | date: 'mediumDate' }}</p>
                </div>
              </article>
            }
          </section>

          <section class="panel" id="qa">
            <h2>Questions &amp; answers</h2>
            @if (item.enrolled) {
              <form class="form stacked" (submit)="submitQuestion($event)">
                <label>Question <input name="qtitle" [(ngModel)]="questionTitle" required /></label>
                <label>Details <textarea name="qbody" rows="3" [(ngModel)]="questionBody" required></textarea></label>
                <button class="btn" type="submit" [disabled]="busy()">Ask</button>
              </form>
            } @else {
              <p class="muted">Enroll to ask a question. You can still read the thread below.</p>
            }
            @for (question of questions(); track question.id) {
              <article class="qa">
                <h3>{{ question.title }}</h3>
                <p>{{ question.body }}</p>
                <p class="muted">{{ question.authorName }} · {{ question.createdAt | date: 'mediumDate' }}</p>
                @for (answer of question.answers; track answer.id) {
                  <div class="answer" [class.teacher]="answer.isTeacher">
                    <strong>{{ answer.authorName }}</strong>
                    @if (answer.isTeacher) {
                      <span class="pill" data-status="Published">Instructor</span>
                    }
                    <p>{{ answer.body }}</p>
                  </div>
                }
                @if (item.enrolled) {
                  <form class="inline-reply" (submit)="submitAnswer($event, question)">
                    <input name="reply-{{ question.id }}" [(ngModel)]="replies[question.id]" placeholder="Write an answer" />
                    <button class="btn secondary" type="submit">Reply</button>
                  </form>
                }
              </article>
            }
          </section>
          @if (error()) {
            <p class="error">{{ error() }}</p>
          }
        </div>
      </div>
    } @else if (error()) {
      <p class="error">{{ error() }}</p>
    } @else {
      <p class="muted">Loading…</p>
    }
  `,
})
export class CourseDetail {
  private readonly api = inject(CatalogApi);
  private readonly route = inject(ActivatedRoute);
  readonly session = inject(SessionService);
  readonly course = signal<CourseDetailDto | null>(null);
  readonly curriculum = signal<CurriculumDto | null>(null);
  readonly reviews = signal<ReviewDto[]>([]);
  readonly questions = signal<QuestionDto[]>([]);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  readonly starSlots = starSlots;
  readonly hoursLabel = hoursLabel;
  reviewRating = 5;
  reviewTitle = '';
  reviewBody = '';
  questionTitle = '';
  questionBody = '';
  replies: Record<string, string> = {};

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Missing course id.');
      return;
    }
    void this.load(id);
  }

  initials(name: string): string {
    return name.split(' ').map((part) => part[0]).join('').slice(0, 2).toUpperCase();
  }

  async submitReview(event: Event): Promise<void> {
    event.preventDefault();
    const id = this.course()?.id;
    if (!id || !this.reviewBody.trim()) {
      return;
    }
    this.busy.set(true);
    try {
      await this.api.addReview(id, {
        rating: this.reviewRating,
        title: this.reviewTitle.trim(),
        body: this.reviewBody.trim(),
      });
      this.reviewBody = '';
      this.reviewTitle = '';
      await this.load(id);
    } catch {
      this.error.set('Could not save your review. Confirm you are enrolled.');
    } finally {
      this.busy.set(false);
    }
  }

  async submitQuestion(event: Event): Promise<void> {
    event.preventDefault();
    const id = this.course()?.id;
    if (!id || !this.questionTitle.trim() || !this.questionBody.trim()) {
      return;
    }
    this.busy.set(true);
    try {
      await this.api.addQuestion(id, { title: this.questionTitle.trim(), body: this.questionBody.trim() });
      this.questionTitle = '';
      this.questionBody = '';
      this.questions.set(await this.api.questions(id));
    } catch {
      this.error.set('Could not post the question. Confirm you are enrolled.');
    } finally {
      this.busy.set(false);
    }
  }

  async submitAnswer(event: Event, question: QuestionDto): Promise<void> {
    event.preventDefault();
    const id = this.course()?.id;
    const body = (this.replies[question.id] ?? '').trim();
    if (!id || !body) {
      return;
    }
    this.busy.set(true);
    try {
      const updated = await this.api.addAnswer(id, question.id, body);
      this.replies[question.id] = '';
      this.questions.update((items) => items.map((item) => (item.id === updated.id ? updated : item)));
    } catch {
      this.error.set('Could not post the answer.');
    } finally {
      this.busy.set(false);
    }
  }

  private async load(id: string): Promise<void> {
    try {
      const [course, curriculum, reviews, questions] = await Promise.all([
        this.api.course(id),
        this.api.curriculum(id),
        this.api.reviews(id),
        this.api.questions(id),
      ]);
      this.course.set(course);
      this.curriculum.set(curriculum);
      this.reviews.set(reviews);
      this.questions.set(questions);
    } catch {
      this.error.set('Course was not found.');
    }
  }
}
