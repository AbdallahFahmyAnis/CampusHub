import { Injectable, computed, signal } from '@angular/core';
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

@Injectable({ providedIn: 'root' })
export class NotificationAlerts {
  private readonly unread = signal(0);
  private readonly items = signal<NotificationDto[]>([]);
  readonly unreadCount = this.unread.asReadonly();
  readonly all = this.items.asReadonly();
  readonly recent = computed(() => this.items().slice(0, 8));

  constructor(private readonly http: HttpClient) {}

  async refresh(): Promise<void> {
    try {
      const [count, list] = await Promise.all([
        firstValueFrom(this.http.get<{ count: number }>('/api/notifications/unread-count')),
        firstValueFrom(this.http.get<NotificationDto[]>('/api/notifications/mine')),
      ]);
      this.unread.set(count.count);
      this.items.set(list);
    } catch {
      this.unread.set(0);
      this.items.set([]);
    }
  }

  async markRead(id: string): Promise<void> {
    await firstValueFrom(this.http.post(`/api/notifications/${id}/read`, {}));
    await this.refresh();
  }

  async markAllRead(): Promise<void> {
    await firstValueFrom(this.http.post('/api/notifications/read-all', {}));
    await this.refresh();
  }
}
