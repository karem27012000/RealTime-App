import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { User } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/users`;

  me(): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/me`);
  }

  getAll(): Observable<User[]> {
    return this.http.get<User[]>(this.baseUrl);
  }
}
