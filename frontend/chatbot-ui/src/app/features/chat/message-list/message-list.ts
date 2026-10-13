import { Component, effect, ElementRef, input, viewChild } from '@angular/core';

import { Message } from '../../../core/models/message.model';

/**
 * Shows the messages of the active conversation and the reply the bot is writing.
 */
@Component({
  selector: 'app-message-list',
  templateUrl: './message-list.html',
  styleUrl: './message-list.scss',
})
export class MessageList {
  readonly messages = input.required<Message[]>();
  readonly streamingReply = input<string | null>(null);
  readonly isLoading = input(false);

  private readonly scrollContainer = viewChild.required<ElementRef<HTMLElement>>('scrollContainer');

  constructor() {
    // Keep the newest message in view whenever messages change or a new chunk arrives.
    effect(() => {
      this.messages();
      this.streamingReply();
      const element = this.scrollContainer().nativeElement;
      queueMicrotask(() => (element.scrollTop = element.scrollHeight));
    });
  }
}
