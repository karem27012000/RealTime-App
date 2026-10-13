/** Matches MessageDto in the .NET API. */
export interface Message {
  id: string;
  roomId: string;
  senderId: string;
  senderNickname: string;
  content: string;
  createdAt: string;
}

/** Matches TypingDto in the .NET API. */
export interface Typing {
  roomId: string;
  userId: string;
  nickname: string;
}
