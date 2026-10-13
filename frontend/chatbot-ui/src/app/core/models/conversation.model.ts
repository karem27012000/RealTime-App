import { Message } from './message.model';

/** Matches ConversationDto in the .NET API. */
export interface Conversation {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
}

/** Matches ConversationDetailsDto in the .NET API. */
export interface ConversationDetails extends Conversation {
  messages: Message[];
}
