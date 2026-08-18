import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface ChatRoomDto {
  id: string;
  title: string;
  kind: 'campus' | 'course';
  courseId?: string;
}

export interface ChatMessageDto {
  id: string;
  clientId: string;
  roomId: string;
  body: string;
  senderId: string;
  senderName: string;
  sentAt: string;
}

@Injectable({ providedIn: 'root' })
export class ChatApi {
  private readonly http = inject(HttpClient);

  rooms() {
    return firstValueFrom(this.http.get<ChatRoomDto[]>('/api/chat/rooms'));
  }

  messages(roomId: string) {
    return firstValueFrom(
      this.http.get<ChatMessageDto[]>(`/api/chat/rooms/${encodeURIComponent(roomId)}/messages`),
    );
  }
}
