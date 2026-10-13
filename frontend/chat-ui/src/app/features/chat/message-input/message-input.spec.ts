import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MessageInput } from './message-input';

describe('MessageInput', () => {
  let fixture: ComponentFixture<MessageInput>;
  let textarea: HTMLTextAreaElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [MessageInput] }).compileComponents();
    fixture = TestBed.createComponent(MessageInput);
    await fixture.whenStable();
    textarea = fixture.nativeElement.querySelector('textarea');
  });

  async function type(value: string): Promise<void> {
    textarea.value = value;
    textarea.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  it('emits the trimmed text when Enter is pressed', async () => {
    const sent: string[] = [];
    fixture.componentInstance.send.subscribe((text) => sent.push(text));

    await type('  Hello  ');
    textarea.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

    expect(sent).toEqual(['Hello']);
  });

  it('does not send empty messages', async () => {
    const sent: string[] = [];
    fixture.componentInstance.send.subscribe((text) => sent.push(text));

    await type('   ');
    textarea.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

    expect(sent).toEqual([]);
  });

  it('does not send while disconnected', async () => {
    const sent: string[] = [];
    fixture.componentInstance.send.subscribe((text) => sent.push(text));
    fixture.componentRef.setInput('disabled', true);

    await type('Hello');
    textarea.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

    expect(sent).toEqual([]);
  });

  it('reports typing at most once every 2 seconds', async () => {
    let typingEvents = 0;
    fixture.componentInstance.typing.subscribe(() => typingEvents++);

    await type('H');
    await type('He');
    await type('Hel');

    expect(typingEvents).toBe(1);
  });
});
