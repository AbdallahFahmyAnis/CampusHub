/** SDD CH-S15 / MDP-26 — teacher gradebook. /catalog/:id/gradebook */
import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CatalogApi, GradebookDto } from './catalog.api';

@Component({
  selector: 'app-course-gradebook',
  imports: [RouterLink, DecimalPipe],
  template: `
    <a class="back-link" routerLink="/catalog/mine">Back to my courses</a>
    <div class="page-head">
      <div>
        <h1>{{ book()?.courseTitle || 'Gradebook' }}</h1>
        <p class="page-kicker">Quiz percents and assignment scores for students who have submitted work.</p>
      </div>
      @if (courseId(); as id) {
        <div class="actions" style="display:flex;gap:.5rem;flex-wrap:wrap;">
          <a class="btn secondary" [routerLink]="['/catalog', id, 'edit']">Edit course</a>
          <a class="btn secondary" [routerLink]="['/catalog', id, 'roster']">Roster</a>
          <a class="btn secondary" [routerLink]="['/catalog', id, 'analytics']">Analytics</a>
        </div>
      }
    </div>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    } @else if (!book()) {
      <p class="muted">Loading gradebook…</p>
    } @else if (book(); as g) {
      @if (g.columns.length === 0) {
        <div class="empty">
          <p class="empty-title">No graded work yet</p>
          <p class="muted">Add a quiz or assignment on the course editor, then scores appear here.</p>
        </div>
      } @else {
        <section class="panel" style="overflow:auto;">
          <table style="width:100%;border-collapse:collapse;min-width:480px;">
            <thead>
              <tr>
                <th style="text-align:left;padding:.6rem .4rem;">Student</th>
                @for (col of g.columns; track col.id) {
                  <th style="text-align:right;padding:.6rem .4rem;">
                    {{ col.title }}
                    <span class="muted" style="font-weight:400;"> · {{ col.kind === 'quiz' ? '%' : col.maxScore + ' pts' }}</span>
                  </th>
                }
                <th style="text-align:right;padding:.6rem .4rem;">Overall</th>
              </tr>
            </thead>
            <tbody>
              @for (row of g.rows; track row.studentId) {
                <tr style="border-top:1px solid var(--border,#e5e7eb);">
                  <td style="padding:.6rem .4rem;">{{ row.studentName }}</td>
                  @for (cell of row.cells; track cell.itemId; let i = $index) {
                    <td style="text-align:right;padding:.6rem .4rem;">
                      @if (cell.score != null) {
                        @if (g.columns[i].kind === 'quiz') {
                          {{ cell.score }}%
                        } @else {
                          {{ cell.score }} / {{ cell.maxScore }}
                        }
                      } @else if (cell.submitted) {
                        <span class="muted">Ungraded</span>
                      } @else {
                        <span class="muted">—</span>
                      }
                    </td>
                  }
                  <td style="text-align:right;padding:.6rem .4rem;font-weight:600;">
                    @if (row.percent != null) {
                      {{ row.percent | number:'1.0-1' }}%
                    } @else {
                      <span class="muted">—</span>
                    }
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td [attr.colspan]="g.columns.length + 2" class="muted" style="padding:1rem .4rem;">
                    No student submissions yet.
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </section>
      }
    }
  `,
})
export class CourseGradebook {
  private readonly api = inject(CatalogApi);
  private readonly route = inject(ActivatedRoute);
  readonly book = signal<GradebookDto | null>(null);
  readonly error = signal<string | null>(null);
  readonly courseId = signal<string | null>(null);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    this.courseId.set(id);
    if (id) {
      void this.api.gradebook(id)
        .then((g) => this.book.set(g))
        .catch(() => this.error.set('Could not load the gradebook. Confirm you own this course.'));
    }
  }
}
