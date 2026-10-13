import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Conversation } from '../../../core/models/conversation.model';

/**
 * Sidebar with the list of conversations. Presentational: data in, events out.
 */
@Component({
  selector: 'app-conversation-list',
  imports: [RouterLink, DatePipe],
  templateUrl: './conversation-list.html',
  styleUrl: './conversation-list.scss',
})
export class ConversationList {
  readonly conversations = input.required<Conversation[]>();
  readonly activeId = input<string | null>(null);

  readonly deleted = output<string>();

  protected onDelete(event: Event, conversation: Conversation): void {
    // The delete button sits inside the link, so stop the click from navigating.
    event.preventDefault();
    event.stopPropagation();

    if (confirm(`Delete "${conversation.title}"?`)) {
      this.deleted.emit(conversation.id);
    }
  }
}
