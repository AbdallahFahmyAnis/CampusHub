/** SDD CH-S11–S16 teacher authoring: quizzes, assignments, due dates, announcements. */
import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CatalogApi, AnnouncementDto, AssignmentSubmissionDto, AssignmentSummaryDto, CurriculumDto, QuizSummaryDto, SubjectDto } from './catalog.api';

@Component({
  selector: 'app-course-editor',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DatePipe],
  template: `
    <a class="back-link" routerLink="/catalog/mine">Back to my courses</a>
    <div class="page-head">
      <div>
        <h1>{{ id() ? 'Edit course' : 'Create course' }}</h1>
        <p class="page-kicker">{{ id() ? 'Update the syllabus, seats, and price, then publish when it is ready.' : 'Start as a draft. Students see it only after you publish.' }}</p>
      </div>
      @if (id(); as courseId) {
        <div class="actions" style="display:flex;gap:.5rem;flex-wrap:wrap;align-items:center;">
          @if (status(); as current) {
            <span class="pill" [attr.data-status]="current">{{ current }}</span>
          }
          <a class="btn secondary" [routerLink]="['/catalog', courseId, 'gradebook']">Gradebook</a>
          <a class="btn secondary" [routerLink]="['/catalog', courseId, 'analytics']">Analytics</a>
        </div>
      } @else if (status(); as current) {
        <span class="pill" [attr.data-status]="current">{{ current }}</span>
      }
    </div>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    <form class="form" [formGroup]="form" (ngSubmit)="save()">
      <label>
        Category
        <select formControlName="subjectId">
          <option value="">Select a category</option>
          @for (subject of subjects(); track subject.id) {
            <option [value]="subject.id">{{ subject.code }} — {{ subject.name }}</option>
          }
        </select>
      </label>
      <label>
        Title
        <input formControlName="title" />
      </label>
      <label>
        Description
        <textarea rows="5" formControlName="description"></textarea>
      </label>
      <label>
        Subtitle
        <input formControlName="subtitle" />
      </label>
      <label>
        Level
        <select formControlName="level">
          <option value="Beginner">Beginner</option>
          <option value="Intermediate">Intermediate</option>
          <option value="Advanced">Advanced</option>
        </select>
      </label>
      <label>
        Language
        <input formControlName="language" />
      </label>
      <label>
        What students will learn (one line each)
        <textarea rows="5" formControlName="outcomes"></textarea>
      </label>
      <label>
        Requirements (one line each)
        <textarea rows="3" formControlName="requirements"></textarea>
      </label>
      <label>
        Capacity
        <input type="number" min="1" formControlName="capacity" />
      </label>
      <label>
        Price (USD)
        <input type="number" min="0" step="0.01" formControlName="price" />
      </label>
      <div class="actions">
        <button class="btn" type="submit" [disabled]="form.invalid || saving()">Save</button>
        @if (id() && status() !== 'Published') {
          <button class="btn secondary" type="button" (click)="publish()" [disabled]="saving()">Publish</button>
        }
      </div>
    </form>
    @if (id(); as courseId) {
      <section class="panel" style="margin-top: 24px; max-width: 720px;">
        <h2>Curriculum</h2>
        <p class="muted">Add sections and lectures. Mark a lecture as preview to unlock it before enrollment.</p>
        @for (section of curriculum()?.sections ?? []; track section.id) {
          <details open>
            <summary>{{ section.title }}</summary>
            <ul>
              @for (lecture of section.lectures; track lecture.id) {
                <li>{{ lecture.title }} · {{ lecture.kind }} · {{ lecture.durationMinutes }}m @if (lecture.isPreview) { (preview) }</li>
              }
            </ul>
            <form class="form stacked" (submit)="addLecture($event, section.id)">
              <label>Lecture title <input name="ltitle-{{ section.id }}" [(ngModel)]="lectureTitle[section.id]" /></label>
              <label>Kind
                <select name="lkind-{{ section.id }}" [(ngModel)]="lectureKind[section.id]">
                  <option value="Article">Article</option>
                  <option value="Video">Video</option>
                </select>
              </label>
              <label>Video URL (YouTube, Vimeo, or .mp4)
                <input name="lurl-{{ section.id }}" [(ngModel)]="lectureVideo[section.id]" placeholder="https://www.youtube.com/watch?v=..." />
              </label>
              <label>Minutes <input type="number" name="mins-{{ section.id }}" [(ngModel)]="lectureMinutes[section.id]" /></label>
              <label>Body <textarea name="lbody-{{ section.id }}" rows="3" [(ngModel)]="lectureBody[section.id]"></textarea></label>
              <label class="inline"><input type="checkbox" name="prev-{{ section.id }}" [(ngModel)]="lecturePreview[section.id]" /> Preview</label>
              <button class="btn secondary" type="submit">Add lecture</button>
            </form>
          </details>
        }
        <form class="form stacked" (submit)="addSection($event)">
          <label>New section <input name="sectionTitle" [(ngModel)]="sectionTitle" /></label>
          <button class="btn" type="submit">Add section</button>
        </form>
      </section>
      <section class="panel" style="margin-top: 24px; max-width: 720px;">
        <h2>Quizzes</h2>
        <p class="muted">Multiple-choice checkpoints. Students take them in the course player.</p>
        @for (quiz of quizzes(); track quiz.id) {
          <p>
            <strong>{{ quiz.title }}</strong>
            <span class="muted"> · {{ quiz.questionCount }} questions · pass at {{ quiz.passPercent }}%</span>
          </p>
        }
        <form class="form stacked" (submit)="addQuiz($event)">
          <label>Quiz title <input name="quizTitle" [(ngModel)]="quizTitle" required /></label>
          <label>Pass mark (%) <input type="number" min="1" max="100" name="passPct" [(ngModel)]="quizPass" /></label>
          @for (q of quizQuestions; track $index; let qi = $index) {
            <fieldset class="panel" style="padding: 0.75rem;">
              <legend>Question {{ qi + 1 }}</legend>
              <label>Prompt <input [(ngModel)]="q.prompt" [name]="'qp-' + qi" /></label>
              @for (choice of q.choices; track $index; let c = $index) {
                <label class="inline">
                  <input type="radio" [name]="'correct-' + qi" [checked]="q.correctIndex === c" (change)="q.correctIndex = c" />
                  <input [(ngModel)]="q.choices[c]" [name]="'qc-' + qi + '-' + c" placeholder="Choice {{ c + 1 }}" />
                </label>
              }
            </fieldset>
          }
          <button class="btn" type="submit">Add quiz</button>
        </form>
      </section>
      <section class="panel" style="margin-top: 24px; max-width: 720px;">
        <h2>Assignments</h2>
        <p class="muted">Written work. Students submit from the course player; you grade here.</p>
        @for (a of assignments(); track a.id) {
          <details>
            <summary>{{ a.title }} <span class="muted">· {{ a.submissionCount }} submission{{ a.submissionCount === 1 ? '' : 's' }} · {{ a.maxScore }} pts
              @if (a.dueAt) { · due {{ a.dueAt | date: 'mediumDate' }} }</span></summary>
            <p>{{ a.instructions }}</p>
            @for (sub of assignmentSubs[a.id] ?? []; track sub.id) {
              <article class="qa">
                <strong>{{ sub.studentName }}</strong>
                <p>{{ sub.body }}</p>
                @if (sub.score != null) {
                  <p class="muted">Score {{ sub.score }} / {{ a.maxScore }}
                    @if (sub.feedback) { · {{ sub.feedback }} }
                  </p>
                }
                <form class="inline-reply" (submit)="grade($event, a.id, sub.id)">
                  <input type="number" [name]="'sc-' + sub.id" [(ngModel)]="gradeScore[sub.id]" [placeholder]="'0–' + a.maxScore" />
                  <input [name]="'fb-' + sub.id" [(ngModel)]="gradeFeedback[sub.id]" placeholder="Feedback" />
                  <button class="btn secondary" type="submit">Grade</button>
                </form>
              </article>
            }
            <button type="button" class="btn secondary" (click)="loadSubs(a.id)">Refresh submissions</button>
          </details>
        }
        <form class="form stacked" (submit)="addAssignment($event)">
          <label>Title <input name="atitle" [(ngModel)]="assignmentTitle" /></label>
          <label>Instructions <textarea name="ainstr" rows="3" [(ngModel)]="assignmentInstructions"></textarea></label>
          <label>Max score <input type="number" name="amax" [(ngModel)]="assignmentMax" /></label>
          <label>Due date (optional) <input type="datetime-local" name="adue" [(ngModel)]="assignmentDue" /></label>
          <button class="btn" type="submit">Add assignment</button>
        </form>
      </section>
      <section class="panel" style="margin-top: 24px; max-width: 720px;">
        <h2>Announcements</h2>
        <p class="muted">Posts appear in the course player for enrolled students.</p>
        @for (post of announcements(); track post.id) {
          <article class="qa">
            <h3>{{ post.title }}</h3>
            <p>{{ post.body }}</p>
            <p class="muted">{{ post.authorName }}</p>
          </article>
        }
        <form class="form stacked" (submit)="addAnnouncement($event)">
          <label>Title <input name="ntitle" [(ngModel)]="announcementTitle" /></label>
          <label>Message <textarea name="nbody" rows="3" [(ngModel)]="announcementBody"></textarea></label>
          <button class="btn" type="submit">Post announcement</button>
        </form>
      </section>
    }
  `,
})
export class CourseEditor {
  private readonly api = inject(CatalogApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly id = signal<string | null>(this.route.snapshot.paramMap.get('id'));
  readonly subjects = signal<SubjectDto[]>([]);
  readonly status = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly curriculum = signal<CurriculumDto | null>(null);
  readonly quizzes = signal<QuizSummaryDto[]>([]);
  readonly assignments = signal<AssignmentSummaryDto[]>([]);
  readonly announcements = signal<AnnouncementDto[]>([]);
  readonly saving = signal(false);
  sectionTitle = '';
  quizTitle = '';
  quizPass = 70;
  quizQuestions = [
    { prompt: '', choices: ['', '', '', ''], correctIndex: 0 },
    { prompt: '', choices: ['', '', '', ''], correctIndex: 0 },
  ];
  assignmentTitle = '';
  assignmentInstructions = '';
  assignmentMax = 100;
  assignmentDue = '';
  announcementTitle = '';
  announcementBody = '';
  assignmentSubs: Record<string, AssignmentSubmissionDto[]> = {};
  gradeScore: Record<string, number> = {};
  gradeFeedback: Record<string, string> = {};
  lectureTitle: Record<string, string> = {};
  lectureKind: Record<string, string> = {};
  lectureVideo: Record<string, string> = {};
  lectureMinutes: Record<string, number> = {};
  lectureBody: Record<string, string> = {};
  lecturePreview: Record<string, boolean> = {};

  readonly form = this.fb.nonNullable.group({
    subjectId: ['', Validators.required],
    title: ['', Validators.required],
    description: [''],
    subtitle: [''],
    level: ['Beginner'],
    language: ['English'],
    outcomes: [''],
    requirements: [''],
    capacity: [20, [Validators.required, Validators.min(1)]],
    price: [0, [Validators.required, Validators.min(0)]],
  });

  constructor() {
    void this.bootstrap();
  }

  private async bootstrap(): Promise<void> {
    this.subjects.set(await this.api.subjects());
    const id = this.id();
    if (!id) {
      return;
    }
    const course = await this.api.course(id);
    this.status.set(course.status);
    this.curriculum.set(await this.api.curriculum(id));
    this.quizzes.set(await this.api.quizzes(id));
    this.assignments.set(await this.api.assignments(id).catch(() => []));
    this.announcements.set(await this.api.announcements(id).catch(() => []));
    this.form.patchValue({
      subjectId: course.subjectId,
      title: course.title,
      description: course.description ?? '',
      subtitle: course.subtitle ?? '',
      level: course.level ?? 'Beginner',
      language: course.language ?? 'English',
      outcomes: (course.outcomes ?? []).join('\n'),
      requirements: (course.requirements ?? []).join('\n'),
      capacity: course.capacity,
      price: course.price,
    });
  }

  async save(): Promise<void> {
    if (this.form.invalid) {
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    const value = this.form.getRawValue();
    try {
      const id = this.id();
      const saved = id
        ? await this.api.update(id, value)
        : await this.api.create(value);
      await this.router.navigate(['/catalog', saved.id, 'edit']);
      this.id.set(saved.id);
      this.status.set(saved.status);
      this.curriculum.set(await this.api.curriculum(saved.id));
    } catch {
      this.error.set('Could not save the course.');
    } finally {
      this.saving.set(false);
    }
  }

  async publish(): Promise<void> {
    const id = this.id();
    if (!id) {
      return;
    }
    this.saving.set(true);
    try {
      const saved = await this.api.publish(id);
      this.status.set(saved.status);
    } catch {
      this.error.set('Could not publish the course.');
    } finally {
      this.saving.set(false);
    }
  }

  async addSection(event: Event): Promise<void> {
    event.preventDefault();
    const id = this.id();
    if (!id || !this.sectionTitle.trim()) {
      return;
    }
    await this.api.addSection(id, this.sectionTitle.trim());
    this.sectionTitle = '';
    this.curriculum.set(await this.api.curriculum(id));
  }

  async addLecture(event: Event, sectionId: string): Promise<void> {
    event.preventDefault();
    const id = this.id();
    const title = (this.lectureTitle[sectionId] ?? '').trim();
    if (!id || !title) {
      return;
    }
    await this.api.addLecture(id, sectionId, {
      title,
      durationMinutes: this.lectureMinutes[sectionId] || 10,
      body: this.lectureBody[sectionId],
      isPreview: this.lecturePreview[sectionId] === true,
      kind: this.lectureKind[sectionId] || 'Article',
      videoUrl: (this.lectureVideo[sectionId] ?? '').trim() || undefined,
    });
    this.lectureTitle[sectionId] = '';
    this.lectureBody[sectionId] = '';
    this.lectureVideo[sectionId] = '';
    this.curriculum.set(await this.api.curriculum(id));
  }

  async addQuiz(event: Event): Promise<void> {
    event.preventDefault();
    const id = this.id();
    const questions = this.quizQuestions
      .filter((q) => q.prompt.trim() && q.choices.filter((c) => c.trim()).length >= 2)
      .map((q) => {
        const choices = q.choices.map((c) => c.trim()).filter(Boolean);
        return {
          prompt: q.prompt.trim(),
          choices,
          correctIndex: Math.min(q.correctIndex, choices.length - 1),
        };
      });
    if (!id || !this.quizTitle.trim() || questions.length === 0) {
      this.error.set('Add a title and at least one complete question.');
      return;
    }
    try {
      await this.api.createQuiz(id, {
        title: this.quizTitle.trim(),
        passPercent: this.quizPass || 70,
        questions,
      });
      this.quizTitle = '';
      this.quizQuestions = [
        { prompt: '', choices: ['', '', '', ''], correctIndex: 0 },
        { prompt: '', choices: ['', '', '', ''], correctIndex: 0 },
      ];
      this.quizzes.set(await this.api.quizzes(id));
      this.error.set(null);
    } catch {
      this.error.set('Could not save the quiz.');
    }
  }

  async addAssignment(event: Event): Promise<void> {
    event.preventDefault();
    const id = this.id();
    if (!id || !this.assignmentTitle.trim() || !this.assignmentInstructions.trim()) {
      this.error.set('Add an assignment title and instructions.');
      return;
    }
    try {
      await this.api.createAssignment(id, {
        title: this.assignmentTitle.trim(),
        instructions: this.assignmentInstructions.trim(),
        maxScore: this.assignmentMax || 100,
        dueAt: this.assignmentDue ? new Date(this.assignmentDue).toISOString() : null,
      });
      this.assignmentTitle = '';
      this.assignmentInstructions = '';
      this.assignmentDue = '';
      this.assignments.set(await this.api.assignments(id));
      this.error.set(null);
    } catch {
      this.error.set('Could not save the assignment.');
    }
  }

  async addAnnouncement(event: Event): Promise<void> {
    event.preventDefault();
    const id = this.id();
    if (!id || !this.announcementTitle.trim() || !this.announcementBody.trim()) {
      this.error.set('Add an announcement title and message.');
      return;
    }
    try {
      await this.api.createAnnouncement(id, {
        title: this.announcementTitle.trim(),
        body: this.announcementBody.trim(),
      });
      this.announcementTitle = '';
      this.announcementBody = '';
      this.announcements.set(await this.api.announcements(id));
      this.error.set(null);
    } catch {
      this.error.set('Could not post the announcement.');
    }
  }

  async loadSubs(assignmentId: string): Promise<void> {
    const id = this.id();
    if (!id) {
      return;
    }
    this.assignmentSubs[assignmentId] = await this.api.assignmentSubmissions(id, assignmentId);
  }

  async grade(event: Event, assignmentId: string, submissionId: string): Promise<void> {
    event.preventDefault();
    const id = this.id();
    if (!id) {
      return;
    }
    try {
      const updated = await this.api.gradeAssignment(id, assignmentId, submissionId, {
        score: Number(this.gradeScore[submissionId] ?? 0),
        feedback: this.gradeFeedback[submissionId],
      });
      this.assignmentSubs[assignmentId] = (this.assignmentSubs[assignmentId] ?? []).map((s) =>
        s.id === updated.id ? updated : s,
      );
    } catch {
      this.error.set('Could not save the grade.');
    }
  }
}
