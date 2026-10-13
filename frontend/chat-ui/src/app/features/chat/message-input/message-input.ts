import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

/** How often (at most) we tell the others "I'm typing". */
const TYPING_THROTTLE_MS = 2000;

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
  static readonly maxLength = 2000; // same limit as Message.ContentMaxLength in the API

  readonly disabled = input(false);

  readonly send = output<string>();
  readonly typing = output<void>();

  protected readonly text = signal('');
  protected readonly maxLength = MessageInput.maxLength;

  private lastTypingSentAt = 0;

  protected onInput(value: string): void {
    this.text.set(value);

    const now = Date.now();
    if (value.trim() && now - this.lastTypingSentAt > TYPING_THROTTLE_MS) {
      this.lastTypingSentAt = now;
      this.typing.emit();
    }
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.submit();
    }
  }

  protected submit(): void {
    const value = this.text().trim();
    if (!value || this.disabled()) {
      return;
    }

    this.send.emit(value);
    this.text.set('');
    this.lastTypingSentAt = 0;
  }
}
