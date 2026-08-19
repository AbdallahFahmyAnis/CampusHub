import { DatePipe } from '@angular/common';
import { Component, OnDestroy, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import {
  CatalogApi,
  CourseDetailDto,
  CurriculumDto,
  LectureDetailDto,
  QuestionDto,
  ReviewDto,
  starSlots,
} from '../../../catalog-mfe/src/app/catalog.api';
import { VideoEmbed } from '../../../catalog-mfe/src/app/video-embed';
import { SessionService } from '../../../shell/src/app/session';

@Component({
  selector: 'app-course-player',
  imports: [RouterLink, DatePipe, FormsModule, VideoEmbed],
  template: `
    @if (course(); as item) {
      <div class="player">
        <aside class="player-nav">
          <a class="back-link light" [routerLink]="['/catalog', item.id]">Course landing</a>
          <h1>{{ item.title }}</h1>
          <div class="player-progress">
            {{ progress().done }} / {{ progress().total }} complete
            <div class="player-progress-bar" aria-hidden="true"><span [style.width.%]="progress().pct"></span></div>
          </div>
          @if (progress().pct === 100) {
            <div class="completion-banner">
              <span aria-hidden="true">🎓</span>
              <strong>Course complete!</strong>
              <a routerLink="/learn/certificates">View certificate</a>
            </div>
          }
          @for (section of curriculum()?.sections ?? []; track section.id) {
            <p class="player-section">{{ section.title }}</p>
            @for (lecture of section.lectures; track lecture.id) {
              <a
                class="player-item"
                [class.active]="lecture.id === lectureId()"
                [class.done]="lecture.completed"
                [routerLink]="['/learn', 'course', item.id, lecture.id]"
              >
                <span>{{ lecture.completed ? '✓' : lecture.kind === 'Video' ? '▶' : '☰' }} {{ lecture.title }}</span>
                <span class="muted">{{ lecture.durationMinutes }}m</span>
              </a>
            }
          }
        </aside>
        <section class="player-main">
          <div class="player-tabs">
            <button type="button" [class.active]="tab() === 'lecture'" (click)="tab.set('lecture')">Lecture</button>
            <button type="button" [class.active]="tab() === 'notes'" (click)="tab.set('notes')">Notes</button>
            <button type="button" [class.active]="tab() === 'ask'" (click)="tab.set('ask')">Ask AI</button>
            <button type="button" [class.active]="tab() === 'qa'" (click)="tab.set('qa')">Q&amp;A</button>
            <button type="button" [class.active]="tab() === 'reviews'" (click)="tab.set('reviews')">Reviews</button>
          </div>

          @if (tab() === 'lecture') {
            @if (lecture(); as current) {
              <div class="stage" [class.video]="current.kind === 'Video'">
                @if (current.kind === 'Video' && !current.locked && current.videoUrl) {
                  <app-video-embed [url]="current.videoUrl">
                    <p>Lecture · {{ current.durationMinutes }} min</p>
                    <strong>{{ current.title }}</strong>
                  </app-video-embed>
                } @else {
                  <p>Lecture · {{ current.durationMinutes }} min</p>
                  <strong>{{ current.title }}</strong>
                }
              </div>
              <h2>{{ current.title }}</h2>
              @if (current.locked) {
                <div class="empty">
                  <p class="empty-title">This lecture is locked</p>
                  <p class="muted">Preview lectures are free. Enroll to open the rest of the curriculum.</p>
                  <a class="btn" [routerLink]="['/enroll', item.id]">Enroll now</a>
                </div>
              } @else {
                <p class="muted">{{ current.summary }}</p>
                @for (para of paragraphs(current.body); track $index) {
                  <p>{{ para }}</p>
                }
                <div class="complete-row">
                  <button type="button" class="btn secondary" [disabled]="current.completed" (click)="markComplete()">
                    {{ current.completed ? 'Completed' : 'Mark as complete' }}
                  </button>
                </div>
              }
            } @else {
              <p class="muted">Choose a lecture from the sidebar.</p>
            }
          }

          @if (tab() === 'notes') {
            @if (lecture(); as current) {
              @if (current.locked) {
                <p class="muted">Enroll to take notes on this lecture.</p>
              } @else {
                <label>Your notes
                  <textarea class="notes-box" rows="10" [(ngModel)]="noteDraft" (ngModelChange)="saveNote()" placeholder="Capture a timestamp, a question, or the idea you want to keep."></textarea>
                </label>
              }
            }
          }

          @if (tab() === 'ask') {
            @if (!allowModelAi()) {
              <p class="muted">Free plan answers come from this course’s materials. Upgrade to Campus for the AI tutor.</p>
            } @else {
              <p class="muted">Ask about this lecture. Answers stay inside the course materials{{ tutorSource() === 'model' ? ' and an AI model.' : '.' }}</p>
            }
            <form class="form stacked" (submit)="askTutor($event)">
              <label>Question
                <textarea name="ask" rows="3" [(ngModel)]="askQuestion" required placeholder="What is the main idea of this lecture?"></textarea>
              </label>
              <button class="btn" type="submit" [disabled]="askBusy()">Ask</button>
            </form>
            @if (askAnswer()) {
              <article class="qa">
                <h3>Answer</h3>
                <p>{{ askAnswer() }}</p>
              </article>
            }
          }

          @if (tab() === 'qa') {
            @if (item.enrolled) {
              <form class="form stacked" (submit)="ask($event)">
                <label>Question <input name="title" [(ngModel)]="questionTitle" required /></label>
                <label>Details <textarea name="body" rows="3" [(ngModel)]="questionBody" required></textarea></label>
                <button class="btn" type="submit">Ask</button>
              </form>
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
                  <form class="inline-reply" (submit)="reply($event, question)">
                    <input name="reply-{{ question.id }}" [(ngModel)]="replies[question.id]" placeholder="Write an answer" />
                    <button class="btn secondary" type="submit">Reply</button>
                  </form>
                }
              </article>
            }
          }

          @if (tab() === 'reviews') {
            @if (item.enrolled) {
              <form class="form stacked" (submit)="review($event)">
                <div class="star-input">
                  @for (n of [1, 2, 3, 4, 5]; track n) {
                    <button type="button" class="star-btn" [class.on]="n <= reviewRating" (click)="reviewRating = n">★</button>
                  }
                </div>
                <label>Headline <input name="rtitle" [(ngModel)]="reviewTitle" /></label>
                <label>Review <textarea name="rbody" rows="4" [(ngModel)]="reviewBody" required></textarea></label>
                <button class="btn" type="submit">Submit review</button>
              </form>
            }
            @for (itemReview of reviews(); track itemReview.id) {
              <article class="review">
                <div>
                  <strong>{{ itemReview.studentName }}</strong>
                  <div class="stars">
                    @for (slot of starSlots(itemReview.rating); track $index) {
                      <span [class.on]="slot.on">★</span>
                    }
                  </div>
                  @if (itemReview.title) {
                    <h3>{{ itemReview.title }}</h3>
                  }
                  <p>{{ itemReview.body }}</p>
                </div>
              </article>
            }
          }
          @if (error()) {
            <p class="error">{{ error() }}</p>
          }
        </section>
      </div>
    } @else if (error()) {
      <p class="error">{{ error() }}</p>
    } @else {
      <p class="muted">Loading course…</p>
    }
  `,
})
export class CoursePlayer implements OnDestroy {
  private readonly api = inject(CatalogApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly sub: Subscription;
  readonly course = signal<CourseDetailDto | null>(null);
  readonly curriculum = signal<CurriculumDto | null>(null);
  readonly lecture = signal<LectureDetailDto | null>(null);
  readonly lectureId = signal<string | null>(null);
  readonly reviews = signal<ReviewDto[]>([]);
  readonly questions = signal<QuestionDto[]>([]);
  readonly tab = signal<'lecture' | 'notes' | 'ask' | 'qa' | 'reviews'>('lecture');
  readonly error = signal<string | null>(null);
  readonly starSlots = starSlots;
  readonly askBusy = signal(false);
  readonly askAnswer = signal<string | null>(null);
  readonly tutorSource = signal('catalog');
  readonly session = inject(SessionService);
  readonly allowModelAi = computed(() => this.session.session().plan !== 'free');
  readonly progress = computed(() => {
    const lectures = (this.curriculum()?.sections ?? []).flatMap((section) => section.lectures);
    const total = lectures.length;
    const done = lectures.filter((lecture) => lecture.completed).length;
    return { done, total, pct: total ? Math.round((100 * done) / total) : 0 };
  });
  questionTitle = '';
  questionBody = '';
  reviewRating = 5;
  reviewTitle = '';
  reviewBody = '';
  replies: Record<string, string> = {};
  noteDraft = '';
  askQuestion = '';

  constructor() {
    void this.api.capabilities()
      .then((caps) => this.tutorSource.set(caps.tutor))
      .catch(() => undefined);
    this.sub = this.route.paramMap.subscribe((params) => {
      const courseId = params.get('courseId');
      const lectureId = params.get('lectureId');
      if (courseId) {
        void this.open(courseId, lectureId);
      }
    });
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  paragraphs(body?: string | null): string[] {
    return (body ?? '').split(/\n\n+/).map((part) => part.trim()).filter(Boolean);
  }

  async askTutor(event: Event): Promise<void> {
    event.preventDefault();
    const courseId = this.course()?.id;
    const question = this.askQuestion.trim();
    if (!courseId || !question) {
      return;
    }
    this.askBusy.set(true);
    this.error.set(null);
    try {
      const result = await this.api.ask(courseId, { question, lectureId: this.lecture()?.id });
      this.askAnswer.set(result.answer);
      this.tutorSource.set(result.source);
    } catch {
      this.error.set('Could not answer from this course yet.');
    } finally {
      this.askBusy.set(false);
    }
  }

  async markComplete(): Promise<void> {
    const courseId = this.course()?.id;
    const lecture = this.lecture();
    if (!courseId || !lecture || lecture.locked || lecture.completed) {
      return;
    }
    try {
      await this.api.completeLecture(courseId, lecture.id);
      this.lecture.set({ ...lecture, completed: true });
      this.curriculum.update((curriculum) => {
        if (!curriculum) {
          return curriculum;
        }
        return {
          ...curriculum,
          sections: curriculum.sections.map((section) => ({
            ...section,
            lectures: section.lectures.map((item) =>
              item.id === lecture.id ? { ...item, completed: true } : item,
            ),
          })),
        };
      });
      // If the course is now 100% done, navigate the tab to the lecture view
      // so the completion banner in the sidebar is visible
      this.tab.set('lecture');
    } catch {
      this.error.set('Could not mark this lecture complete. Confirm you can open it.');
    }
  }

  saveNote(): void {
    const courseId = this.course()?.id;
    const lectureId = this.lecture()?.id;
    if (!courseId || !lectureId) {
      return;
    }
    localStorage.setItem(this.noteKey(courseId, lectureId), this.noteDraft);
  }

  private noteKey(courseId: string, lectureId: string): string {
    return `campushub:notes:${courseId}:${lectureId}`;
  }

  async ask(event: Event): Promise<void> {
    event.preventDefault();
    const id = this.course()?.id;
    if (!id) {
      return;
    }
    await this.api.addQuestion(id, { title: this.questionTitle.trim(), body: this.questionBody.trim() });
    this.questionTitle = '';
    this.questionBody = '';
    this.questions.set(await this.api.questions(id));
  }

  async reply(event: Event, question: QuestionDto): Promise<void> {
    event.preventDefault();
    const id = this.course()?.id;
    const body = (this.replies[question.id] ?? '').trim();
    if (!id || !body) {
      return;
    }
    const updated = await this.api.addAnswer(id, question.id, body);
    this.replies[question.id] = '';
    this.questions.update((items) => items.map((item) => (item.id === updated.id ? updated : item)));
  }

  async review(event: Event): Promise<void> {
    event.preventDefault();
    const id = this.course()?.id;
    if (!id) {
      return;
    }
    await this.api.addReview(id, { rating: this.reviewRating, title: this.reviewTitle.trim(), body: this.reviewBody.trim() });
    this.reviewBody = '';
    this.reviews.set(await this.api.reviews(id));
  }

  private async open(courseId: string, lectureId: string | null): Promise<void> {
    try {
      if (!this.course() || this.course()?.id !== courseId) {
        const [course, curriculum, reviews, questions] = await Promise.all([
          this.api.course(courseId),
          this.api.curriculum(courseId),
          this.api.reviews(courseId),
          this.api.questions(courseId),
        ]);
        this.course.set(course);
        this.curriculum.set(curriculum);
        this.reviews.set(reviews);
        this.questions.set(questions);
      }
      const first = this.curriculum()?.sections[0]?.lectures[0]?.id ?? null;
      const target = lectureId ?? first;
      if (!lectureId && target) {
        await this.router.navigate(['/learn', 'course', courseId, target], { replaceUrl: true });
        return;
      }
      this.lectureId.set(target);
      if (target) {
        const detail = await this.api.lecture(courseId, target);
        this.lecture.set(detail);
        this.noteDraft = localStorage.getItem(this.noteKey(courseId, target)) ?? '';
      }
      this.error.set(null);
    } catch {
      this.error.set('Could not open this course.');
    }
  }
}
