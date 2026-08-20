import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface EnrollmentDto {
  id: string;
  courseId: string;
  courseTitle: string;
  amount: number;
  status: string;
  paymentId?: string | null;
  failureReason?: string | null;
  createdAt: string;
  updatedAt: string;
}

/** SDD CH-S23 — specs/023-course-waitlist */
export interface WaitlistEntryDto {
  id: string;
  courseId: string;
  courseTitle: string;
  position: number;
  queueLength: number;
  joinedAt: string;
}

export interface WaitlistStatusDto {
  waitlisted: boolean;
  position: number | null;
  queueLength: number;
}

@Injectable({ providedIn: 'root' })
export class EnrollmentApi {
  private readonly http = inject(HttpClient);

  start(courseId: string, simulatePayment: 'Succeeded' | 'Failed') {
    return firstValueFrom(
      this.http.post<EnrollmentDto>('/api/enrollments', { courseId, simulatePayment }),
    );
  }

  get(id: string) {
    return firstValueFrom(this.http.get<EnrollmentDto>(`/api/enrollments/${id}`));
  }

  mine() {
    return firstValueFrom(this.http.get<EnrollmentDto[]>('/api/enrollments/mine'));
  }

  waitlistMine() {
    return firstValueFrom(this.http.get<WaitlistEntryDto[]>('/api/enrollments/waitlist/mine'));
  }

  waitlistStatus(courseId: string) {
    return firstValueFrom(this.http.get<WaitlistStatusDto>(`/api/enrollments/waitlist/courses/${courseId}`));
  }

  joinWaitlist(courseId: string) {
    return firstValueFrom(this.http.post<WaitlistEntryDto>(`/api/enrollments/waitlist/courses/${courseId}`, {}));
  }

  leaveWaitlist(courseId: string) {
    return firstValueFrom(this.http.delete<WaitlistStatusDto>(`/api/enrollments/waitlist/courses/${courseId}`));
  }
}
