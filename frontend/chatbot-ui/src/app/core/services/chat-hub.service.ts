import { Injectable } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

/**
 * Real-time connection to the .NET SignalR hub (/hubs/chat).
 * The bot's reply arrives as a stream of text chunks.
 */
@Injectable({ providedIn: 'root' })
export class ChatHubService {
  private readonly connection: HubConnection = new HubConnectionBuilder()
    .withUrl(`${environment.apiUrl}/hubs/chat`)
    .withAutomaticReconnect()
    .configureLogging(environment.production ? LogLevel.Warning : LogLevel.Information)
    .build();

  private connecting?: Promise<void>;

  /**
   * Sends a message and emits each chunk of the bot's reply.
   * Unsubscribing cancels the reply on the server.
   */
  streamReply(conversationId: string, content: string): Observable<string> {
    return new Observable<string>((subscriber) => {
      let subscription: { dispose(): void } | undefined;
      let unsubscribed = false;

      this.ensureConnected()
        .then(() => {
          if (unsubscribed) {
            return;
          }
          subscription = this.connection
            .stream<string>('SendMessage', conversationId, content)
            .subscribe({
              next: (chunk) => subscriber.next(chunk),
              complete: () => subscriber.complete(),
              error: (err) => subscriber.error(err),
            });
        })
        .catch((err) => subscriber.error(err));

      return () => {
        unsubscribed = true;
        subscription?.dispose();
      };
    });
  }

  private ensureConnected(): Promise<void> {
    if (this.connection.state === HubConnectionState.Connected) {
      return Promise.resolve();
    }

    // Reuse the same promise if a connection attempt is already running.
    this.connecting ??= this.connection.start().finally(() => (this.connecting = undefined));
    return this.connecting;
  }
}
