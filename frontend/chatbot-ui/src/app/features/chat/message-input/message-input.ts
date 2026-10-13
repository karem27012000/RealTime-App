import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

/**
 * Text box at the bottom of the chat. Enter sends, Shift+Enter adds a new line.
 */
@Component({
  selector: 'app-message-input',
  imports: [FormsModule],
  templateUrl: './message-input.html',
  styleUrl: './message-input.scss',
})
export class MessageInput {
  static readonly maxLength = 4000; // same limit as ChatService.MaxMessageLength in the API

  readonly isBotReplying = input(false);

  readonly send = output<string>();
  readonly stop = output<void>();

  protected readonly text = signal('');
  protected readonly maxLength = MessageInput.maxLength;

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.submit();
    }
  }

  protected submit(): void {
    const value = this.text().trim();
    if (!value || this.isBotReplying()) {
      return;
    }

    this.send.emit(value);
    this.text.set('');
  }
}
