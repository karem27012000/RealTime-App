import { DatePipe } from '@angular/common';
import { Component, effect, ElementRef, input, output, untracked, viewChild } from '@angular/core';

import { Message } from '../../../core/models/message.model';

/**
 * Shows the messages of the open room. My messages are on the right.
 */
@Component({
  selector: 'app-message-list',
  imports: [DatePipe],
  templateUrl: './message-list.html',
  styleUrl: './message-list.scss',
})
export class MessageList {
  readonly messages = input.required<Message[]>();
  readonly currentUserId = input<string | null>(null);
  readonly isGroup = input(false);
  readonly isLoading = input(false);
  readonly hasOlder = input(false);

  readonly loadOlder = output<void>();

  private readonly scrollContainer = viewChild.required<ElementRef<HTMLElement>>('scrollContainer');
  private lastMessageId: string | undefined;

  constructor() {
    // Scroll to the bottom when a new message arrives at the end
    // (but not when older messages are added at the top).
    effect(() => {
      const messages = this.messages();
      const newest = messages.at(-1)?.id;
      if (newest === this.lastMessageId) return;
      this.lastMessageId = newest;

      const element = untracked(() => this.scrollContainer().nativeElement);
      queueMicrotask(() => (element.scrollTop = element.scrollHeight));
    });
  }

  /** Show the sender name only on the first of several messages in a row from the same person. */
  protected showSender(index: number): boolean {
    const list = this.messages();
    const message = list[index];
    return (
      this.isGroup() &&
      message.senderId !== this.currentUserId() &&
      list[index - 1]?.senderId !== message.senderId
    );
  }

  /** Show a date separator when the day changes. */
  protected showDate(index: number): boolean {
    const list = this.messages();
    return (
      index === 0 ||
      new Date(list[index - 1].createdAt).toDateString() !==
        new Date(list[index].createdAt).toDateString()
    );
  }
}
