import { DatePipe } from '@angular/common';
import { Component, Input, OnChanges, OnDestroy, SimpleChanges, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { io, Socket } from 'socket.io-client';
import { ChatMessageDto } from './chat.api';
import { SessionService } from '../../../shell/src/app/session';

type PanelMode = 'chat' | 'tutor';

@Component({
  selector: 'app-course-chat-panel',
  imports: [DatePipe, FormsModule],
  template: `
    <div class="course-chat-panel">
      <div class="chat-panel-tabs">
        <button class="tab-btn" [class.active]="mode() === 'chat'" (click)="switchMode('chat')">
          Live chat
        </button>
        <button class="tab-btn" [class.active]="mode() === 'tutor'" (click)="switchMode('tutor')">
          AI Tutor
        </button>
      </div>
      @if (error()) {
        <p class="error" style="padding: .5rem 1rem; font-size: .85rem;">{{ error() }}</p>
      }
      @if (!connected()) {
        <p class="muted" style="padding: .75rem 1rem; font-size: .85rem;">Connecting…</p>
      }
      @if (connected()) {
        <div class="chat-log compact-log" #scrollEl>
          @for (item of messages(); track item.id) {
            <article class="chat-line" [class.mine]="item.senderId === me()" [class.bot]="item.senderId === 'ai-tutor'">
              <div class="meta-line">
                <strong>{{ item.senderName }}</strong>
                <span class="muted">{{ item.sentAt | date: 'shortTime' }}</span>
              </div>
              <p>{{ item.body }}</p>
            </article>
          } @empty {
            <p class="muted" style="padding: .75rem 1rem; font-size: .85rem;">
              {{ mode() === 'tutor' ? 'Ask the AI a question about this course.' : 'No messages yet.' }}
            </p>
          }
        </div>
        <form class="chat-compose compact" (submit)="send($event)">
          <input
            [(ngModel)]="draft"
            name="draft"
            [placeholder]="mode() === 'tutor' ? 'Ask the AI tutor…' : 'Write a message…'"
            autocomplete="off"
          />
          <button class="btn" type="submit" [disabled]="!draft.trim() || sending()">
            {{ sending() ? '…' : 'Send' }}
          </button>
        </form>
      }
    </div>
  `,
  styles: [`
    .course-chat-panel {
      display: flex;
      flex-direction: column;
      height: 420px;
      border: 1px solid var(--border, #e5e7eb);
      border-radius: 8px;
      overflow: hidden;
      background: var(--surface, #fff);
    }
    .chat-panel-tabs {
      display: flex;
      border-bottom: 1px solid var(--border, #e5e7eb);
    }
    .tab-btn {
      flex: 1;
      padding: .6rem 1rem;
      background: none;
      border: none;
      cursor: pointer;
      font-size: .9rem;
      color: var(--muted, #6b7280);
      border-bottom: 2px solid transparent;
    }
    .tab-btn.active {
      color: var(--accent, #4f46e5);
      border-bottom-color: var(--accent, #4f46e5);
      font-weight: 600;
    }
    .compact-log {
      flex: 1;
      overflow-y: auto;
      padding: .5rem 1rem;
    }
    .chat-line { margin-bottom: .75rem; }
    .chat-line p { margin: .1rem 0 0; font-size: .9rem; line-height: 1.4; }
    .chat-line.mine p { color: var(--accent, #4f46e5); }
    .chat-line.bot .meta-line strong { color: #16a34a; }
    .meta-line { display: flex; gap: .5rem; align-items: baseline; font-size: .75rem; }
    .meta-line strong { font-weight: 600; font-size: .85rem; }
    .compact { display: flex; gap: .5rem; padding: .6rem; border-top: 1px solid var(--border, #e5e7eb); }
    .compact input { flex: 1; padding: .4rem .7rem; border: 1px solid var(--border, #e5e7eb); border-radius: 6px; font-size: .9rem; }
    .compact .btn { padding: .4rem .9rem; font-size: .85rem; }
  `],
})
export class CourseChatPanel implements OnChanges, OnDestroy {
  @Input({ required: true }) courseId!: string;

  private readonly session = inject(SessionService);
  private socket?: Socket;
  private currentRoomId: string | null = null;

  readonly mode = signal<PanelMode>('chat');
  readonly messages = signal<ChatMessageDto[]>([]);
  readonly error = signal<string | null>(null);
  readonly connected = signal(false);
  readonly me = signal<string | null>(null);
  readonly sending = signal(false);
  draft = '';

  constructor() {
    void this.session.load().then((s) => this.me.set(s.sub));
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['courseId'] && this.courseId) {
      if (!this.socket) {
        this.connect();
      } else {
        this.joinRoom();
      }
    }
  }

  ngOnDestroy(): void {
    this.socket?.removeAllListeners();
    this.socket?.disconnect();
  }

  switchMode(m: PanelMode): void {
    if (this.mode() === m) return;
    this.mode.set(m);
    this.messages.set([]);
    this.joinRoom();
  }

  async send(event: Event): Promise<void> {
    event.preventDefault();
    const body = this.draft.trim();
    if (!body || !this.socket || !this.currentRoomId) return;
    this.draft = '';
    this.sending.set(this.mode() === 'tutor');
    this.socket.emit(
      'message',
      { roomId: this.currentRoomId, body, clientId: crypto.randomUUID() },
      (result: { ok: boolean; error?: string }) => {
        this.sending.set(false);
        if (!result?.ok) {
          this.error.set(result?.error ?? 'Could not send message.');
        }
      },
    );
  }

  private connect(): void {
    this.socket = io({
      path: '/socket.io',
      withCredentials: true,
      transports: ['websocket', 'polling'],
    });
    this.socket.on('connect', () => {
      this.connected.set(true);
      this.joinRoom();
    });
    this.socket.on('message', (msg: ChatMessageDto) => {
      if (msg.roomId !== this.currentRoomId) return;
      this.messages.update((items) =>
        items.some((m) => m.id === msg.id || m.clientId === msg.clientId) ? items : [...items, msg],
      );
      this.sending.set(false);
    });
    this.socket.on('connect_error', () => {
      this.error.set('Chat connection failed. Make sure you are signed in through the gateway.');
    });
  }

  private joinRoom(): void {
    const roomId = this.mode() === 'tutor'
      ? `tutor:${this.courseId}`
      : `course:${this.courseId}`;
    this.currentRoomId = roomId;
    this.messages.set([]);
    this.error.set(null);
    this.socket?.emit(
      'join',
      roomId,
      (result: { ok: boolean; messages?: ChatMessageDto[]; error?: string }) => {
        if (!result?.ok) {
          this.error.set(result?.error ?? 'Could not join room.');
          return;
        }
        this.messages.set(result.messages ?? []);
      },
    );
  }
}
