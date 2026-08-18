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

@Injectable({ providedIn: 'root' })
export class CatalogApi {
  private readonly http = inject(HttpClient);

  subjects() {
    return firstValueFrom(this.http.get<SubjectDto[]>('/api/catalog/subjects'));
  }

  courses(options?: { category?: string; page?: number; pageSize?: number }) {
    const params: Record<string, string | number> = {
      page: options?.page ?? 1,
      pageSize: options?.pageSize ?? 12,
    };
    if (options?.category) {
      params['category'] = options.category;
    }

    return firstValueFrom(this.http.get<PagedCoursesDto>('/api/catalog/courses', { params }));
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
