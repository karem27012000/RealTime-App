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

    await type('  Hello bot  ');
    textarea.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

    expect(sent).toEqual(['Hello bot']);
  });

  it('does not send empty messages', async () => {
    const sent: string[] = [];
    fixture.componentInstance.send.subscribe((text) => sent.push(text));

    await type('   ');
    textarea.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

    expect(sent).toEqual([]);
  });

  it('shows a Stop button while the bot is replying', async () => {
    fixture.componentRef.setInput('isBotReplying', true);
    await fixture.whenStable();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.textContent?.trim()).toBe('Stop');
  });
});
