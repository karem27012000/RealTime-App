import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Conversation, ConversationDetails } from '../models/conversation.model';

/**
 * Talks to the REST endpoints of the .NET API (/api/conversations).
 */
@Injectable({ providedIn: 'root' })
export class ConversationApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/conversations`;

  getAll(): Observable<Conversation[]> {
    return this.http.get<Conversation[]>(this.baseUrl);
  }

  getById(id: string): Observable<ConversationDetails> {
    return this.http.get<ConversationDetails>(`${this.baseUrl}/${id}`);
  }

  create(title?: string): Observable<Conversation> {
    return this.http.post<Conversation>(this.baseUrl, { title: title ?? null });
  }

  rename(id: string, title: string): Observable<Conversation> {
    return this.http.patch<Conversation>(`${this.baseUrl}/${id}`, { title });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
