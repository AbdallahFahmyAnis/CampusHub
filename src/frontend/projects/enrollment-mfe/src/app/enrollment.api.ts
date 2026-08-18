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
}
