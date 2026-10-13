/** Matches UserDto in the .NET API. */
export interface User {
  id: string;
  nickname: string;
  isOnline: boolean;
  lastSeenAt: string;
}
