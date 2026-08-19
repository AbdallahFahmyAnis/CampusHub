import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface SubjectDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
}

export interface CourseListItemDto {
  id: string;
  title: string;
  subtitle?: string | null;
  subjectCode: string;
  subjectName: string;
  teacherName: string;
  capacity: number;
  remainingSeats: number;
  price: number;
  status: string;
  level?: string | null;
  ratingAverage: number;
  ratingCount: number;
  lectureCount: number;
  durationMinutes: number;
  wishlisted?: boolean;
}

export interface PagedCoursesDto {
  items: CourseListItemDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CourseDetailDto extends CourseListItemDto {
  subjectId: string;
  description?: string | null;
  teacherId: string;
  teacherEmail: string;
  canEnroll: boolean;
  enrolled: boolean;
  language?: string | null;
  outcomes: string[];
  requirements: string[];
}

export interface UpsertCourseRequest {
  subjectId: string;
  title: string;
  description?: string | null;
  capacity: number;
  price: number;
  subtitle?: string | null;
  level?: string | null;
  language?: string | null;
  outcomes?: string | null;
  requirements?: string | null;
}

export interface LectureOutlineDto {
  id: string;
  title: string;
  kind: string;
  durationMinutes: number;
  summary?: string | null;
  isPreview: boolean;
  sortOrder: number;
  videoUrl?: string | null;
  completed?: boolean;
}

export interface SectionDto {
  id: string;
  title: string;
  sortOrder: number;
  lectures: LectureOutlineDto[];
}

export interface CurriculumDto {
  courseId: string;
  sections: SectionDto[];
}

export interface LectureDetailDto {
  id: string;
  sectionId: string;
  courseId: string;
  title: string;
  kind: string;
  durationMinutes: number;
  summary?: string | null;
  body?: string | null;
  isPreview: boolean;
  locked: boolean;
  sortOrder: number;
  videoUrl?: string | null;
  completed?: boolean;
}

export interface ReviewDto {
  id: string;
  studentName: string;
  rating: number;
  title?: string | null;
  body: string;
  createdAt: string;
  mine: boolean;
}

export interface AnswerDto {
  id: string;
  authorName: string;
  body: string;
  isTeacher: boolean;
  createdAt: string;
}

export interface QuestionDto {
  id: string;
  authorName: string;
  title: string;
  body: string;
  createdAt: string;
  answers: AnswerDto[];
}

export interface LectureStatDto {
  id: string;
  title: string;
  sectionTitle: string;
  durationMinutes: number;
  completionCount: number;
}

export interface MonthlyEnrollmentDto {
  month: string;
  count: number;
  revenue: number;
}

export interface CourseStatsDto {
  courseId: string;
  courseTitle: string;
  totalLectures: number;
  studentsCompletedAll: number;
  averageRating: number;
  reviewCount: number;
  lectureStats: LectureStatDto[];
  totalEnrollments: number;
  confirmedEnrollments: number;
  cancelledEnrollments: number;
  totalRevenue: number;
  monthlyBreakdown: MonthlyEnrollmentDto[];
}

export interface QuizChoiceDto {
  index: number;
  text: string;
}

export interface QuizQuestionDto {
  id: string;
  prompt: string;
  choices: QuizChoiceDto[];
  correctIndex?: number | null;
}

export interface QuizSummaryDto {
  id: string;
  title: string;
  passPercent: number;
  questionCount: number;
  bestScore: number | null;
  passed: boolean | null;
}

export interface QuizDetailDto {
  id: string;
  title: string;
  passPercent: number;
  questions: QuizQuestionDto[];
  bestScore: number | null;
  passed: boolean | null;
}

export interface QuizAttemptDto {
  id: string;
  score: number;
  total: number;
  percent: number;
  passed: boolean;
  submittedAt: string;
}

export interface AssignmentSummaryDto {
  id: string;
  title: string;
  instructions: string;
  maxScore: number;
  submitted: boolean;
  score: number | null;
  feedback: string | null;
  submissionCount: number;
}

export interface AssignmentSubmissionDto {
  id: string;
  assignmentId: string;
  studentId: string;
  studentName: string;
  body: string;
  score: number | null;
  feedback: string | null;
  submittedAt: string;
  gradedAt: string | null;
}

export interface LectureNoteDto {
  courseId: string;
  lectureId: string;
  body: string;
  updatedAt: string | null;
}

export interface LectureNoteListItemDto {
  courseId: string;
  courseTitle: string;
  lectureId: string;
  lectureTitle: string;
  snippet: string;
  updatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class CatalogApi {
  private readonly http = inject(HttpClient);

  subjects() {
    return firstValueFrom(this.http.get<SubjectDto[]>('/api/catalog/subjects'));
  }

  courses(options?: {
    category?: string;
    q?: string;
    level?: string;
    minPrice?: number;
    maxPrice?: number;
    minRating?: number;
    sort?: string;
    page?: number;
    pageSize?: number;
  }) {
    const params: Record<string, string | number> = {
      page: options?.page ?? 1,
      pageSize: options?.pageSize ?? 12,
    };
    if (options?.category) params['category'] = options.category;
    if (options?.q) params['q'] = options.q;
    if (options?.level) params['level'] = options.level;
    if (options?.minPrice != null) params['minPrice'] = options.minPrice;
    if (options?.maxPrice != null) params['maxPrice'] = options.maxPrice;
    if (options?.minRating != null) params['minRating'] = options.minRating;
    if (options?.sort) params['sortBy'] = options.sort;

    return firstValueFrom(this.http.get<PagedCoursesDto>('/api/catalog/courses', { params }));
  }

  recommended() {
    return firstValueFrom(this.http.get<CourseListItemDto[]>('/api/catalog/courses/recommended'));
  }

  mine() {
    return firstValueFrom(this.http.get<CourseListItemDto[]>('/api/catalog/courses/mine'));
  }

  course(id: string) {
    return firstValueFrom(this.http.get<CourseDetailDto>(`/api/catalog/courses/${id}`));
  }

  create(body: UpsertCourseRequest) {
    return firstValueFrom(this.http.post<CourseDetailDto>('/api/catalog/courses', body));
  }

  update(id: string, body: UpsertCourseRequest) {
    return firstValueFrom(this.http.put<CourseDetailDto>(`/api/catalog/courses/${id}`, body));
  }

  publish(id: string) {
    return firstValueFrom(this.http.post<CourseDetailDto>(`/api/catalog/courses/${id}/publish`, {}));
  }

  curriculum(id: string) {
    return firstValueFrom(this.http.get<CurriculumDto>(`/api/catalog/courses/${id}/curriculum`));
  }

  lecture(courseId: string, lectureId: string) {
    return firstValueFrom(this.http.get<LectureDetailDto>(`/api/catalog/courses/${courseId}/lectures/${lectureId}`));
  }

  addSection(courseId: string, title: string) {
    return firstValueFrom(this.http.post<SectionDto>(`/api/catalog/courses/${courseId}/sections`, { title }));
  }

  addLecture(courseId: string, sectionId: string, body: {
    title: string;
    kind?: string;
    durationMinutes: number;
    summary?: string;
    body?: string;
    isPreview: boolean;
    videoUrl?: string;
  }) {
    return firstValueFrom(
      this.http.post<LectureOutlineDto>(`/api/catalog/courses/${courseId}/sections/${sectionId}/lectures`, body),
    );
  }

  reviews(id: string) {
    return firstValueFrom(this.http.get<ReviewDto[]>(`/api/catalog/courses/${id}/reviews`));
  }

  addReview(id: string, body: { rating: number; title?: string; body: string }) {
    return firstValueFrom(this.http.post<ReviewDto>(`/api/catalog/courses/${id}/reviews`, body));
  }

  questions(id: string) {
    return firstValueFrom(this.http.get<QuestionDto[]>(`/api/catalog/courses/${id}/questions`));
  }

  addQuestion(id: string, body: { title: string; body: string }) {
    return firstValueFrom(this.http.post<QuestionDto>(`/api/catalog/courses/${id}/questions`, body));
  }

  addAnswer(courseId: string, questionId: string, body: string) {
    return firstValueFrom(
      this.http.post<QuestionDto>(`/api/catalog/courses/${courseId}/questions/${questionId}/answers`, { body }),
    );
  }

  wishlist() {
    return firstValueFrom(this.http.get<CourseListItemDto[]>('/api/catalog/wishlist'));
  }

  addWishlist(courseId: string) {
    return firstValueFrom(this.http.post<{ wishlisted: boolean }>(`/api/catalog/courses/${courseId}/wishlist`, {}));
  }

  removeWishlist(courseId: string) {
    return firstValueFrom(this.http.delete<{ wishlisted: boolean }>(`/api/catalog/courses/${courseId}/wishlist`));
  }

  completeLecture(courseId: string, lectureId: string) {
    return firstValueFrom(
      this.http.post<{ completed: boolean }>(`/api/catalog/courses/${courseId}/lectures/${lectureId}/complete`, {}),
    );
  }

  courseStats(courseId: string) {
    return firstValueFrom(this.http.get<CourseStatsDto>(`/api/catalog/courses/${courseId}/stats`));
  }

  capabilities() {
    return firstValueFrom(this.http.get<{ search: string; tutor: string }>('/api/catalog/capabilities'));
  }

  ask(courseId: string, body: { question: string; lectureId?: string | null }) {
    return firstValueFrom(
      this.http.post<{ answer: string; source: string }>(`/api/catalog/courses/${courseId}/ask`, body),
    );
  }

  quizzes(courseId: string) {
    return firstValueFrom(this.http.get<QuizSummaryDto[]>(`/api/catalog/courses/${courseId}/quizzes`));
  }

  quiz(courseId: string, quizId: string) {
    return firstValueFrom(this.http.get<QuizDetailDto>(`/api/catalog/courses/${courseId}/quizzes/${quizId}`));
  }

  createQuiz(courseId: string, body: {
    title: string;
    passPercent: number;
    questions: { prompt: string; choices: string[]; correctIndex: number }[];
  }) {
    return firstValueFrom(this.http.post<QuizDetailDto>(`/api/catalog/courses/${courseId}/quizzes`, body));
  }

  submitQuiz(courseId: string, quizId: string, answers: { questionId: string; choiceIndex: number }[]) {
    return firstValueFrom(
      this.http.post<QuizAttemptDto>(`/api/catalog/courses/${courseId}/quizzes/${quizId}/submit`, { answers }),
    );
  }

  assignments(courseId: string) {
    return firstValueFrom(this.http.get<AssignmentSummaryDto[]>(`/api/catalog/courses/${courseId}/assignments`));
  }

  createAssignment(courseId: string, body: { title: string; instructions: string; maxScore: number }) {
    return firstValueFrom(this.http.post<AssignmentSummaryDto>(`/api/catalog/courses/${courseId}/assignments`, body));
  }

  submitAssignment(courseId: string, assignmentId: string, body: string) {
    return firstValueFrom(
      this.http.post<AssignmentSubmissionDto>(`/api/catalog/courses/${courseId}/assignments/${assignmentId}/submit`, { body }),
    );
  }

  assignmentSubmissions(courseId: string, assignmentId: string) {
    return firstValueFrom(
      this.http.get<AssignmentSubmissionDto[]>(`/api/catalog/courses/${courseId}/assignments/${assignmentId}/submissions`),
    );
  }

  gradeAssignment(courseId: string, assignmentId: string, submissionId: string, body: { score: number; feedback?: string }) {
    return firstValueFrom(
      this.http.post<AssignmentSubmissionDto>(
        `/api/catalog/courses/${courseId}/assignments/${assignmentId}/submissions/${submissionId}/grade`,
        body,
      ),
    );
  }

  lectureNote(courseId: string, lectureId: string) {
    return firstValueFrom(
      this.http.get<LectureNoteDto>(`/api/catalog/courses/${courseId}/lectures/${lectureId}/notes`),
    );
  }

  saveLectureNote(courseId: string, lectureId: string, body: string) {
    return firstValueFrom(
      this.http.put<LectureNoteDto>(`/api/catalog/courses/${courseId}/lectures/${lectureId}/notes`, { body }),
    );
  }

  myNotes() {
    return firstValueFrom(this.http.get<LectureNoteListItemDto[]>('/api/catalog/notes/mine'));
  }
}

export function starSlots(rating: number): { on: boolean }[] {
  return [1, 2, 3, 4, 5].map((value) => ({ on: value <= Math.round(rating) }));
}

export function hoursLabel(minutes: number): string {
  if (minutes < 60) {
    return `${minutes}m`;
  }
  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;
  return rest ? `${hours}h ${rest}m` : `${hours}h`;
}
