import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom, Subscription } from 'rxjs';

import { Conversation } from '../../core/models/conversation.model';
import { Message } from '../../core/models/message.model';
import { ChatHubService } from '../../core/services/chat-hub.service';
import { ConversationApiService } from '../../core/services/conversation-api.service';

/**
 * Holds the state of the chat screen using Angular signals.
 * Components read the signals and call the methods; they never call the API directly.
 */
@Injectable({ providedIn: 'root' })
export class ChatStore {
  private readonly api = inject(ConversationApiService);
  private readonly hub = inject(ChatHubService);
  private readonly router = inject(Router);

  private streamSubscription?: Subscription;

  // ----- State -----
  readonly conversations = signal<Conversation[]>([]);
  readonly activeConversationId = signal<string | null>(null);
  readonly messages = signal<Message[]>([]);
  /** The bot reply that is currently being streamed (null when the bot is not replying). */
  readonly streamingReply = signal<string | null>(null);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);

  // ----- Derived state -----
  readonly isBotReplying = computed(() => this.streamingReply() !== null);
  readonly activeConversation = computed(
    () => this.conversations().find((c) => c.id === this.activeConversationId()) ?? null,
  );

  async loadConversations(): Promise<void> {
    try {
      this.conversations.set(await firstValueFrom(this.api.getAll()));
    } catch {
      this.error.set('Could not load conversations. Is the API running?');
    }
  }

  async openConversation(id: string | null): Promise<void> {
    if (id === this.activeConversationId()) {
      return;
    }

    this.stopReply();
    this.activeConversationId.set(id);
    this.messages.set([]);
    this.error.set(null);

    if (!id) {
      return;
    }

    this.isLoading.set(true);
    try {
      const details = await firstValueFrom(this.api.getById(id));
      this.messages.set(details.messages);
    } catch {
      this.error.set('Conversation not found.');
      await this.router.navigate(['/']);
    } finally {
      this.isLoading.set(false);
    }
  }

  async deleteConversation(id: string): Promise<void> {
    try {
      await firstValueFrom(this.api.delete(id));
      this.conversations.update((list) => list.filter((c) => c.id !== id));

      if (this.activeConversationId() === id) {
        await this.router.navigate(['/']);
      }
    } catch {
      this.error.set('Could not delete the conversation.');
    }
  }

  async sendMessage(content: string): Promise<void> {
    const text = content.trim();
    if (!text || this.isBotReplying()) {
      return;
    }
    this.error.set(null);

    // Start a new conversation automatically when none is selected.
    let conversationId = this.activeConversationId();
    if (!conversationId) {
      try {
        const created = await firstValueFrom(this.api.create());
        conversationId = created.id;
        this.conversations.update((list) => [created, ...list]);
        this.activeConversationId.set(conversationId);
        await this.router.navigate(['/c', conversationId]);
      } catch {
        this.error.set('Could not start a new conversation.');
        return;
      }
    }

    // Show the user's message straight away (optimistic update).
    this.messages.update((list) => [...list, this.localMessage('user', text)]);
    this.streamingReply.set('');

    this.streamSubscription = this.hub.streamReply(conversationId, text).subscribe({
      next: (chunk) => this.streamingReply.update((reply) => (reply ?? '') + chunk),
      complete: () => {
        this.finishReply();
        // The server may have renamed the chat from the first message, so refresh the sidebar.
        void this.loadConversations();
      },
      error: (err: Error) => {
        this.finishReply();
        this.error.set(err.message?.replace(/^.*HubException: /, '') || 'The bot could not reply.');
      },
    });
  }

  stopReply(): void {
    this.streamSubscription?.unsubscribe();
    this.finishReply();
  }

  clearError(): void {
    this.error.set(null);
  }

  /** Moves the streamed text into the message list. */
  private finishReply(): void {
    const reply = this.streamingReply();
    if (reply) {
      this.messages.update((list) => [...list, this.localMessage('assistant', reply.trim())]);
    }
    this.streamingReply.set(null);
    this.streamSubscription = undefined;
  }

  private localMessage(role: Message['role'], content: string): Message {
    return { id: crypto.randomUUID(), role, content, createdAt: new Date().toISOString() };
  }
}
