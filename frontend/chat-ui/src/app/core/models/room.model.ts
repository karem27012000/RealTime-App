import { Message } from './message.model';
import { User } from './user.model';

export type RoomType = 'private' | 'group';

/** Matches RoomDto in the .NET API (a room as seen by the logged-in user). */
export interface Room {
  id: string;
  /** Group name, or the other user's nickname for private chats. */
  name: string;
  type: RoomType;
  otherUserId: string | null;
  memberCount: number;
  lastMessage: Message | null;
  updatedAt: string;
}

/** Matches RoomDetailsDto in the .NET API. */
export interface RoomDetails {
  id: string;
  name: string;
  type: RoomType;
  otherUserId: string | null;
  createdById: string;
  members: User[];
}
