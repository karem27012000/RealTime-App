import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Message } from '../models/message.model';
import { Room, RoomDetails } from '../models/room.model';

/**
 * REST calls for rooms and message history (/api/rooms).
 * New messages are sent through the SignalR hub, not here.
 */
@Injectable({ providedIn: 'root' })
export class RoomsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/rooms`;

  getMyRooms(): Observable<Room[]> {
    return this.http.get<Room[]>(this.baseUrl);
  }

  getJoinableGroups(): Observable<Room[]> {
    return this.http.get<Room[]>(`${this.baseUrl}/groups`);
  }

  getDetails(roomId: string): Observable<RoomDetails> {
    return this.http.get<RoomDetails>(`${this.baseUrl}/${roomId}`);
  }

  createGroup(name: string): Observable<Room> {
    return this.http.post<Room>(`${this.baseUrl}/groups`, { name });
  }

  startPrivateChat(userId: string): Observable<Room> {
    return this.http.post<Room>(`${this.baseUrl}/private`, { userId });
  }

  join(roomId: string): Observable<Room> {
    return this.http.post<Room>(`${this.baseUrl}/${roomId}/join`, {});
  }

  leave(roomId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${roomId}/leave`, {});
  }

  /** Oldest first. Pass `before` (the oldest date you have) to load an older page. */
  getMessages(roomId: string, before?: string, take = 50): Observable<Message[]> {
    let params = new HttpParams().set('take', take);
    if (before) {
      params = params.set('before', before);
    }
    return this.http.get<Message[]>(`${this.baseUrl}/${roomId}/messages`, { params });
  }
}
