import { Routes } from '@angular/router';
import { Chat } from './chat';

export const CHAT_ROUTES: Routes = [
  { path: '', component: Chat },
  { path: ':roomId', component: Chat },
];
