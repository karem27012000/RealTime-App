import { Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Message, Typing } from '../models/message.model';
import { Room } from '../models/room.model';
import { User } from '../models/user.model';

export type ConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

/**
 * Real-time connection to the .NET SignalR hub (/hubs/chat).
 * Server events are exposed as RxJS Subjects; the method names must match IChatClient in the API.
 */
@Injectable({ providedIn: 'root' })
export class ChatHubService {
  private connection?: HubConnection;

  readonly state = signal<ConnectionState>('disconnected');

  // Events pushed by the server
  readonly messageReceived = new Subject<Message>();
  readonly roomAdded = new Subject<Room>();
  readonly membersChanged = new Subject<{ roomId: string; memberCount: number }>();
  readonly presenceChanged = new Subject<User>();
  readonly userTyping = new Subject<Typing>();
  /** Fires after an automatic reconnect, so the app can reload anything it missed. */
  readonly reconnected = new Subject<void>();

  async connect(userId: string): Promise<void> {
    await this.disconnect();

    const connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/chat?userId=${encodeURIComponent(userId)}`)
      .withAutomaticReconnect()
      .configureLogging(environment.production ? LogLevel.Warning : LogLevel.Information)
      .build();

    connection.on('ReceiveMessage', (message: Message) => this.messageReceived.next(message));
    connection.on('RoomAdded', (room: Room) => this.roomAdded.next(room));
    connection.on('MembersChanged', (roomId: string, memberCount: number) =>
      this.membersChanged.next({ roomId, memberCount }),
    );
    connection.on('PresenceChanged', (user: User) => this.presenceChanged.next(user));
    connection.on('UserTyping', (typing: Typing) => this.userTyping.next(typing));

    connection.onreconnecting(() => this.state.set('reconnecting'));
    connection.onreconnected(() => {
      this.state.set('connected');
      this.reconnected.next();
    });
    connection.onclose(() => this.state.set('disconnected'));

    this.connection = connection;
    this.state.set('connecting');
    try {
      await connection.start();
      this.state.set('connected');
    } catch (err) {
      this.state.set('disconnected');
      throw err;
    }
  }

  async disconnect(): Promise<void> {
    const connection = this.connection;
    this.connection = undefined;
    await connection?.stop();
    this.state.set('disconnected');
  }

  sendMessage(roomId: string, content: string): Promise<Message> {
    return this.requireConnection().invoke<Message>('SendMessage', roomId, content);
  }

  startTyping(roomId: string): Promise<void> {
    return this.requireConnection().send('StartTyping', roomId);
  }

  private requireConnection(): HubConnection {
    if (!this.connection) {
      throw new Error('Not connected to the chat server.');
    }
    return this.connection;
  }
}
