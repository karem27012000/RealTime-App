import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { User } from '../models/user.model';

const STORAGE_KEY = 'chatapp.user';

/**
 * Nickname "login". The user is kept in localStorage so a page refresh keeps you logged in.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  readonly currentUser = signal<User | null>(readStoredUser());
  readonly isLoggedIn = computed(() => this.currentUser() !== null);

  async login(nickname: string): Promise<User> {
    const user = await firstValueFrom(
      this.http.post<User>(`${environment.apiUrl}/api/users/login`, { nickname }),
    );
    this.currentUser.set(user);
    writeStoredUser(user);
    return user;
  }

  logout(): void {
    this.currentUser.set(null);
    writeStoredUser(null);
  }
}

function readStoredUser(): User | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as User) : null;
  } catch {
    return null;
  }
}

function writeStoredUser(user: User | null): void {
  try {
    if (user) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
    } else {
      localStorage.removeItem(STORAGE_KEY);
    }
  } catch {
    // Storage can be unavailable (private mode); the app still works until refresh.
  }
}
