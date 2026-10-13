import { Component, effect, inject, input, OnInit, untracked } from '@angular/core';

import { ChatStore } from '../chat.store';
import { MessageInput } from '../message-input/message-input';
import { MessageList } from '../message-list/message-list';
import { RoomHeader } from '../room-header/room-header';
import { Sidebar } from '../sidebar/sidebar';

/**
 * Page (container) component: connects the store to the presentational components.
 */
@Component({
  selector: 'app-chat-page',
  imports: [Sidebar, RoomHeader, MessageList, MessageInput],
  templateUrl: './chat-page.html',
  styleUrl: './chat-page.scss',
})
export class ChatPage implements OnInit {
  protected readonly store = inject(ChatStore);

  /** Route parameter from /chat/:roomId (bound automatically by withComponentInputBinding). */
  readonly roomId = input<string>();

  constructor() {
    // Re-run only when the route changes. `untracked` stops the effect from also
    // reacting to the store signals that openRoom reads internally.
    effect(() => {
      const roomId = this.roomId() ?? null;
      untracked(() => void this.store.openRoom(roomId));
    });
  }

  ngOnInit(): void {
    void this.store.init();
  }
}
