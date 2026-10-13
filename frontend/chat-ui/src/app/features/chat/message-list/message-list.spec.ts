import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Message } from '../../../core/models/message.model';
import { MessageList } from './message-list';

function message(id: string, senderId: string, senderNickname: string, content: string): Message {
  return {
    id,
    roomId: 'room',
    senderId,
    senderNickname,
    content,
    createdAt: '2026-10-13T10:00:00Z',
  };
}

describe('MessageList', () => {
  let fixture: ComponentFixture<MessageList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [MessageList] }).compileComponents();
    fixture = TestBed.createComponent(MessageList);
    fixture.componentRef.setInput('currentUserId', 'me');
  });

  it('puts my messages on the right and shows sender names in groups', async () => {
    fixture.componentRef.setInput('isGroup', true);
    fixture.componentRef.setInput('messages', [
      message('1', 'bob', 'bob', 'Hi all'),
      message('2', 'bob', 'bob', 'Anyone here?'),
      message('3', 'me', 'karem', 'Yes!'),
    ]);
    await fixture.whenStable();

    const bubbles: HTMLElement[] = Array.from(fixture.nativeElement.querySelectorAll('.bubble'));
    expect(bubbles.map((b) => b.classList.contains('bubble--mine'))).toEqual([false, false, true]);

    // The name is shown once for consecutive messages from the same person, and never for mine.
    const authors = Array.from(fixture.nativeElement.querySelectorAll('.bubble__author')).map((e) =>
      (e as HTMLElement).textContent?.trim(),
    );
    expect(authors).toEqual(['bob']);
  });

  it('shows a hint when there are no messages', async () => {
    fixture.componentRef.setInput('messages', []);
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('No messages yet');
  });
});
