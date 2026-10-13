import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/chat/chat-page/chat-page').then((m) => m.ChatPage),
  },
  {
    path: 'c/:id',
    loadComponent: () => import('./features/chat/chat-page/chat-page').then((m) => m.ChatPage),
  },
  { path: '**', redirectTo: '' },
];
