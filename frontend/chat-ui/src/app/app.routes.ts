import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/guards/auth.guard';

const chatPage = () => import('./features/chat/chat-page/chat-page').then((m) => m.ChatPage);

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'chat' },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/login/login-page/login-page').then((m) => m.LoginPage),
  },
  { path: 'chat', canActivate: [authGuard], loadComponent: chatPage },
  { path: 'chat/:roomId', canActivate: [authGuard], loadComponent: chatPage },
  { path: '**', redirectTo: 'chat' },
];
