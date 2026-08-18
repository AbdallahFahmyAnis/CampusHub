import { DatePipe } from '@angular/common';
import { Component, OnDestroy, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { io, Socket } from 'socket.io-client';
import { ChatApi, ChatMessageDto, ChatRoomDto } from './chat.api';
import { SessionService } from '../../../shell/src/app/session';

@Component({
  selector: 'app-chat',
  imports: [DatePipe, FormsModule, RouterLink],
  template: `
    <div class="page-head">
      <div>
        <h1>Live chat</h1>
        <p class="page-kicker">Course rooms open after a confirmed enrollment. The campus lobby is open to every signed-in user.</p>
      </div>
    </div>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    <div class="chat-layout">
      <aside class="chat-rooms">
        @for (room of rooms(); track room.id) {
          <a class="card" [class.active]="room.id === roomId()" [routerLink]="['/chat', room.id]">
            <h2>{{ room.title }}</h2>
            <span class="pill">{{ room.kind }}</span>
          </a>
        } @empty {
          <p class="muted">Loading rooms…</p>
        }
      </aside>
      <section class="chat-pane card">
        @if (roomId()) {
          <div class="chat-pane-head">
            <h2>{{ title() }}</h2>
          </div>
          <div class="chat-log">
            @for (item of messages(); track item.id) {
              <article class="chat-line" [class.mine]="item.senderId === me()">
                <div class="meta-line">
                  <strong>{{ item.senderName }}</strong>
                  <span class="muted">{{ item.sentAt | date: 'short' }}</span>
                </div>
                <p>{{ item.body }}</p>
              </article>
            } @empty {
              <p class="muted">No messages yet. Say hello.</p>
            }
          </div>
          <form class="chat-compose" (submit)="send($event)">
            <input name="draft" [(ngModel)]="draft" placeholder="Write a message" autocomplete="off" />
            <button class="btn" type="submit" [disabled]="!draft.trim()">Send</button>
          </form>
        } @else {
          <div class="empty chat-empty">
            <p class="empty-title">Choose a room</p>
            <p class="muted">Pick a course or the campus lobby to start talking.</p>
          </div>
        }
      </section>
    </div>
  `,
})
export class Chat implements OnDestroy {
  private readonly api = inject(ChatApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly session = inject(SessionService);
  private socket?: Socket;

  readonly rooms = signal<ChatRoomDto[]>([]);
  readonly messages = signal<ChatMessageDto[]>([]);
  readonly roomId = signal<string | null>(null);
  readonly title = signal('Live chat');
  readonly error = signal<string | null>(null);
  readonly me = signal<string | null>(null);
  draft = '';

  constructor() {
    void this.session.load().then((session) => this.me.set(session.sub));
    void this.bootstrap();
  }

  ngOnDestroy(): void {
    this.socket?.removeAllListeners();
    this.socket?.disconnect();
  }

  async send(event: Event): Promise<void> {
    event.preventDefault();
    const roomId = this.roomId();
    const body = this.draft.trim();
    if (!roomId || !body || !this.socket) {
      return;
    }
    this.draft = '';
    this.socket.emit('message', { roomId, body, clientId: crypto.randomUUID() }, (result: { ok: boolean; error?: string }) => {
      if (!result?.ok) {
        this.error.set(result?.error ?? 'Could not send message.');
      }
    });
  }

  private async bootstrap(): Promise<void> {
    try {
      const rooms = await this.api.rooms();
      this.rooms.set(rooms);
      this.connect();
      this.route.paramMap.subscribe((params) => {
        const requested = params.get('roomId') ?? rooms[0]?.id ?? 'campus';
        if (!params.get('roomId') && rooms[0]) {
          void this.router.navigate(['/chat', rooms[0].id], { replaceUrl: true });
          return;
        }
        this.join(requested);
      });
    } catch {
      this.error.set('Could not load chat rooms. Sign out and sign in again so the gateway requests chat.api.');
    }
  }

  private connect(): void {
    this.socket = io({
      path: '/socket.io',
      withCredentials: true,
      transports: ['websocket', 'polling'],
    });
    this.socket.on('message', (message: ChatMessageDto) => {
      if (message.roomId !== this.roomId()) {
        return;
      }
      this.messages.update((items) =>
        items.some((item) => item.id === message.id || item.clientId === message.clientId)
          ? items
          : [...items, message],
      );
    });
    this.socket.on('connect_error', () => {
      this.error.set('Live connection failed. Confirm you are signed in through the gateway on port 5000.');
    });
  }

  private join(roomId: string): void {
    this.roomId.set(roomId);
    this.messages.set([]);
    const listed = this.rooms().find((room) => room.id === roomId);
    this.title.set(listed?.title ?? 'Live chat');
    this.socket?.emit(
      'join',
      roomId,
      (result: { ok: boolean; title?: string; messages?: ChatMessageDto[]; error?: string }) => {
        if (!result?.ok) {
          this.error.set(result?.error ?? 'Could not join room.');
          return;
        }
        this.title.set(result.title ?? this.title());
        this.messages.set(result.messages ?? []);
        this.error.set(null);
      },
    );
  }
}
