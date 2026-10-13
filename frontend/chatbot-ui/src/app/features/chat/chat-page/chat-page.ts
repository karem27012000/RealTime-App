import { Component, effect, inject, input, OnInit, untracked } from '@angular/core';

import { ChatStore } from '../chat.store';
import { ConversationList } from '../conversation-list/conversation-list';
import { MessageInput } from '../message-input/message-input';
import { MessageList } from '../message-list/message-list';

/**
 * Page (container) component: connects the store to the presentational components.
 */
@Component({
  selector: 'app-chat-page',
  imports: [ConversationList, MessageList, MessageInput],
  templateUrl: './chat-page.html',
  styleUrl: './chat-page.scss',
})
export class ChatPage implements OnInit {
  protected readonly store = inject(ChatStore);

  /** Route parameter from /c/:id (bound automatically by withComponentInputBinding). */
  readonly id = input<string>();

  constructor() {
    // Re-run only when the route id changes. `untracked` stops the effect from also
    // reacting to the store signals that openConversation reads internally.
    effect(() => {
      const id = this.id() ?? null;
      untracked(() => void this.store.openConversation(id));
    });
  }

  ngOnInit(): void {
    void this.store.loadConversations();
  }
}
