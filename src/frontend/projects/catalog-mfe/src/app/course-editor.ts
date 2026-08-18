import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CatalogApi, CurriculumDto, SubjectDto } from './catalog.api';

@Component({
  selector: 'app-course-editor',
  imports: [ReactiveFormsModule, FormsModule, RouterLink],
  template: `
    <a class="back-link" routerLink="/catalog/mine">Back to my courses</a>
    <div class="page-head">
      <div>
        <h1>{{ id() ? 'Edit course' : 'Create course' }}</h1>
        <p class="page-kicker">{{ id() ? 'Update the syllabus, seats, and price, then publish when it is ready.' : 'Start as a draft. Students see it only after you publish.' }}</p>
      </div>
      @if (status(); as current) {
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
                <li>{{ lecture.title }} · {{ lecture.durationMinutes }}m @if (lecture.isPreview) { (preview) }</li>
              }
            </ul>
            <form class="form stacked" (submit)="addLecture($event, section.id)">
              <label>Lecture title <input name="ltitle-{{ section.id }}" [(ngModel)]="lectureTitle[section.id]" /></label>
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
  readonly saving = signal(false);
  sectionTitle = '';
  lectureTitle: Record<string, string> = {};
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
      kind: 'Article',
    });
    this.lectureTitle[sectionId] = '';
    this.lectureBody[sectionId] = '';
    this.curriculum.set(await this.api.curriculum(id));
  }
}
