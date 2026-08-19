import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface NotificationDto {
  id: string;
  title: string;
  body: string;
  eventType: string;
  read: boolean;
  createdAt: string;
  status: string;
}

export interface CredentialDto {
  id: string;
  enrollmentId: string;
  courseId: string;
  courseTitle: string;
  kind: string;
  status: string;
  token: string;
  issuedAt: string;
  expiresAt: string;
}

export interface CourseProgressDto {
  courseId: string;
  courseTitle: string;
  subjectCode: string;
  totalLectures: number;
  completedLectures: number;
  progressPct: number;
  lastActivityAt: string | null;
  continueLectureId: string | null;
  quizCount: number;
  quizzesPassed: number;
  bestQuizPercent: number | null;
}

export interface ProgressDashboardDto {
  courses: CourseProgressDto[];
  streakDays: number;
  totalLecturesCompleted: number;
  lastActivityAt: string | null;
}

export interface ScanDto {
  id: string;
  studentName: string;
  courseTitle: string;
  scannedBy: string;
  scannedAt: string;
}

@Injectable({ providedIn: 'root' })
export class LearningApi {
  private readonly http = inject(HttpClient);

  progressDashboard() {
    return firstValueFrom(this.http.get<ProgressDashboardDto>('/api/catalog/progress/dashboard'));
  }

  mine() {
    return firstValueFrom(this.http.get<NotificationDto[]>('/api/notifications/mine'));
  }

  unreadCount() {
    return firstValueFrom(this.http.get<{ count: number }>('/api/notifications/unread-count'));
  }

  markRead(id: string) {
    return firstValueFrom(this.http.post(`/api/notifications/${id}/read`, {}));
  }

  credentials() {
    return firstValueFrom(this.http.get<CredentialDto[]>('/api/access/credentials/mine'));
  }

  scan(token: string) {
    return firstValueFrom(this.http.post<ScanDto>('/api/access/scans', { token }));
  }

  scans() {
    return firstValueFrom(this.http.get<ScanDto[]>('/api/access/scans'));
  }
}
