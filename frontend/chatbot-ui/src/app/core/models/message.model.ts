export type MessageRole = 'user' | 'assistant';

/** Matches MessageDto in the .NET API. */
export interface Message {
  id: string;
  role: MessageRole;
  content: string;
  createdAt: string;
}
