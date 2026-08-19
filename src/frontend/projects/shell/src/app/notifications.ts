import { Injectable, computed, signal, inject, OnDestroy } from '@angular/core';
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

export function timeAgo(iso: string): string {
  const then = Date.parse(iso);
  if (!Number.isFinite(then)) {
    return '';
  }
  const sec = Math.max(0, Math.round((Date.now() - then) / 1000));
  if (sec < 45) {
    return 'just now';
  }
  const min = Math.round(sec / 60);
  if (min < 60) {
    return `${min}m`;
  }
  const hr = Math.round(min / 60);
  if (hr < 24) {
    return `${hr}h`;
  }
  const day = Math.round(hr / 24);
  if (day < 7) {
    return `${day}d`;
  }
  const week = Math.round(day / 7);
  if (week < 5) {
    return `${week}w`;
  }
  return new Date(then).toLocaleDateString();
}

export type NoticeKind = 'enroll' | 'pay' | 'welcome' | 'access' | 'alert';

export function noticeKind(eventType: string): NoticeKind {
  const type = (eventType ?? '').toLowerCase();
  if (type.includes('enroll')) {
    return 'enroll';
  }
  if (type.includes('pay') || type.includes('payment')) {
    return 'pay';
  }
  if (type.includes('welcome') || type.includes('seed')) {
    return 'welcome';
  }
  if (type.includes('access') || type.includes('pass')) {
    return 'access';
  }
  return 'alert';
}

export function noticeGlyph(kind: NoticeKind): string {
  switch (kind) {
    case 'enroll':
      return '✓';
    case 'pay':
      return '$';
    case 'welcome':
      return '★';
    case 'access':
      return '▣';
    default:
      return '●';
  }
}

@Injectable({ providedIn: 'root' })
export class NotificationAlerts implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly unread = signal(0);
  private readonly items = signal<NotificationDto[]>([]);
  readonly unreadCount = this.unread.asReadonly();
  readonly all = this.items.asReadonly();
  readonly recent = computed(() => this.items().slice(0, 8));

  private sseAbort: AbortController | null = null;

  ngOnDestroy(): void {
    this.stopSse();
  }

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

  /** Connect to the SSE stream. Call once after the user signs in. */
  startSse(): void {
    this.stopSse();
    const abort = new AbortController();
    this.sseAbort = abort;
    void this.connectSse(abort.signal);
  }

  stopSse(): void {
    this.sseAbort?.abort();
    this.sseAbort = null;
  }

  private async connectSse(signal: AbortSignal): Promise<void> {
    // Back-off between reconnect attempts (ms): 2s, 4s, 8s, capped at 30s
    let backoff = 2000;
    while (!signal.aborted) {
      try {
        const response = await fetch('/api/notifications/stream', {
          signal,
          headers: { Accept: 'text/event-stream' },
          credentials: 'include',
        });
        if (!response.ok || !response.body) {
          break;
        }
        backoff = 2000; // reset on success
        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';
        while (!signal.aborted) {
          const { done, value } = await reader.read();
          if (done) {
            break;
          }
          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n');
          buffer = lines.pop() ?? '';
          for (const line of lines) {
            if (line.startsWith('data: ')) {
              const text = line.slice(6).trim();
              if (text && !text.includes('"type":"connected"')) {
                // A real notification arrived — do a lightweight refresh
                void this.refresh();
              }
            }
          }
        }
      } catch {
        if (signal.aborted) {
          return;
        }
      }
      // Wait before reconnecting
      await new Promise<void>((resolve) => setTimeout(resolve, backoff));
      backoff = Math.min(backoff * 2, 30000);
    }
  }

  unreadItems(): NotificationDto[] {
    return this.items().filter((item) => !item.read);
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
