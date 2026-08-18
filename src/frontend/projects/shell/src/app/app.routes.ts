import { Routes } from '@angular/router';
import { adminGuard, authGuard } from './auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'catalog', pathMatch: 'full' },
  {
    path: 'signup',
    loadComponent: () => import('./signup').then((m) => m.SignupPage),
  },
  {
    path: 'invite/:token',
    loadComponent: () => import('./invite').then((m) => m.InvitePage),
  },
  {
    path: 'people',
    canActivate: [adminGuard],
    loadComponent: () => import('./people').then((m) => m.PeoplePage),
  },
  {
    path: 'account',
    canActivate: [authGuard],
    loadComponent: () => import('./profile').then((m) => m.ProfilePage),
  },
  {
    path: 'catalog',
    canActivate: [authGuard],
    loadChildren: () => import('catalog-mfe').then((m) => m.CATALOG_ROUTES),
  },
  {
    path: 'enroll',
    canActivate: [authGuard],
    loadChildren: () => import('enrollment-mfe').then((m) => m.ENROLLMENT_ROUTES),
  },
  {
    path: 'learn',
    canActivate: [authGuard],
    loadChildren: () => import('learning-mfe').then((m) => m.LEARNING_ROUTES),
  },
  {
    path: 'chat',
    canActivate: [authGuard],
    loadChildren: () => import('chat-mfe').then((m) => m.CHAT_ROUTES),
  },
];
