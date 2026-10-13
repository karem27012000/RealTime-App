import { HttpErrorResponse } from '@angular/common/http';
import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { Message, Typing } from '../../core/models/message.model';
import { Room, RoomDetails } from '../../core/models/room.model';
import { User } from '../../core/models/user.model';
import { AuthService } from '../../core/services/auth.service';
import { ChatHubService } from '../../core/services/chat-hub.service';
import { RoomsApiService } from '../../core/services/rooms-api.service';
import { UsersApiService } from '../../core/services/users-api.service';

const PAGE_SIZE = 50;
const TYPING_TIMEOUT_MS = 3000;

/**
 * Holds the state of the chat screen using Angular signals and reacts to real-time
 * events from the SignalR hub. Components read the signals and call the methods;
 * they never call the API directly.
 */
@Injectable({ providedIn: 'root' })
export class ChatStore {
  private readonly auth = inject(AuthService);
  private readonly hub = inject(ChatHubService);
  private readonly roomsApi = inject(RoomsApiService);
  private readonly usersApi = inject(UsersApiService);
  private readonly router = inject(Router);

  private initialized = false;
  private readonly typingTimers = new Map<string, ReturnType<typeof setTimeout>>();

  // ----- State -----
  readonly rooms = signal<Room[]>([]);
  readonly users = signal<User[]>([]);
  readonly joinableGroups = signal<Room[]>([]);
  readonly activeRoomId = signal<string | null>(null);
  readonly activeRoom = signal<RoomDetails | null>(null);
  readonly messages = signal<Message[]>([]);
  readonly hasOlderMessages = signal(false);
  readonly isLoadingMessages = signal(false);
  /** Unread message count per room id. */
  readonly unread = signal<Record<string, number>>({});
  /** Who is typing, per room id (key: `${roomId}:${userId}`). */
  readonly typing = signal<Record<string, Typing>>({});
  readonly error = signal<string | null>(null);

  // ----- Derived state -----
  readonly currentUser = this.auth.currentUser;
  readonly connectionState = this.hub.state;

  /** Everyone except me, online users first. */
  readonly otherUsers = computed(() =>
    this.users()
      .filter((u) => u.id !== this.currentUser()?.id)
      .sort(
        (a, b) => Number(b.isOnline) - Number(a.isOnline) || a.nickname.localeCompare(b.nickname),
      ),
  );

  readonly onlineUserIds = computed(
    () =>
      new Set(
        this.users()
          .filter((u) => u.isOnline)
          .map((u) => u.id),
      ),
  );

  readonly totalUnread = computed(() => Object.values(this.unread()).reduce((a, b) => a + b, 0));

  /** "bob is typing…" for the open room, or null. */
  readonly typingText = computed(() => {
    const roomId = this.activeRoomId();
    const names = Object.values(this.typing())
      .filter((t) => t.roomId === roomId)
      .map((t) => t.nickname);

    if (names.length === 0) return null;
    if (names.length === 1) return `${names[0]} is typing…`;
    return `${names.join(', ')} are typing…`;
  });

  constructor() {
    this.listenToHub(inject(DestroyRef));
  }

  /** Loads the data and opens the real-time connection (once per login). */
  async init(): Promise<void> {
    const user = this.currentUser();
    if (this.initialized || !user) {
      return;
    }
    this.initialized = true;

    try {
      // Make sure the saved login still exists on the server (e.g. the database was reset).
      await firstValueFrom(this.usersApi.me());
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 401) {
        await this.logout();
        return;
      }
    }

    await Promise.all([this.loadRooms(), this.loadUsers()]);

    try {
      await this.hub.connect(user.id);
      // Online status changed while we were connecting, so refresh it.
      await this.loadUsers();
    } catch {
      this.error.set('Could not connect to the chat server. Is the API running?');
    }
  }

  async logout(): Promise<void> {
    await this.hub.disconnect();
    this.auth.logout();
    this.reset();
    await this.router.navigate(['/login']);
  }

  async loadRooms(): Promise<void> {
    try {
      this.rooms.set(await firstValueFrom(this.roomsApi.getMyRooms()));
    } catch {
      this.error.set('Could not load your chats.');
    }
  }

  async loadUsers(): Promise<void> {
    try {
      this.users.set(await firstValueFrom(this.usersApi.getAll()));
    } catch {
      this.error.set('Could not load users.');
    }
  }

  async loadJoinableGroups(): Promise<void> {
    try {
      this.joinableGroups.set(await firstValueFrom(this.roomsApi.getJoinableGroups()));
    } catch {
      this.error.set('Could not load groups.');
    }
  }

  async openRoom(roomId: string | null): Promise<void> {
    if (roomId === this.activeRoomId()) {
      return;
    }

    this.activeRoomId.set(roomId);
    this.activeRoom.set(null);
    this.messages.set([]);
    this.hasOlderMessages.set(false);

    if (!roomId) {
      return;
    }

    this.markRead(roomId);
    this.isLoadingMessages.set(true);
    try {
      const [details, messages] = await Promise.all([
        firstValueFrom(this.roomsApi.getDetails(roomId)),
        firstValueFrom(this.roomsApi.getMessages(roomId, undefined, PAGE_SIZE)),
      ]);

      // The user may have switched rooms while we were loading.
      if (this.activeRoomId() !== roomId) return;

      this.activeRoom.set(details);
      this.messages.set(messages);
      this.hasOlderMessages.set(messages.length === PAGE_SIZE);
    } catch {
      this.error.set('You cannot open this chat.');
      await this.router.navigate(['/chat']);
    } finally {
      this.isLoadingMessages.set(false);
    }
  }

  async loadOlderMessages(): Promise<void> {
    const roomId = this.activeRoomId();
    const oldest = this.messages()[0];
    if (!roomId || !oldest || this.isLoadingMessages()) {
      return;
    }

    this.isLoadingMessages.set(true);
    try {
      const older = await firstValueFrom(
        this.roomsApi.getMessages(roomId, oldest.createdAt, PAGE_SIZE),
      );
      if (this.activeRoomId() !== roomId) return;

      this.messages.update((list) => [...older, ...list]);
      this.hasOlderMessages.set(older.length === PAGE_SIZE);
    } finally {
      this.isLoadingMessages.set(false);
    }
  }

  async sendMessage(content: string): Promise<void> {
    const roomId = this.activeRoomId();
    const text = content.trim();
    if (!roomId || !text) {
      return;
    }

    try {
      // The server pushes the saved message back to every member (including us)
      // through ReceiveMessage; we also add it here in case that event is slower.
      const message = await this.hub.sendMessage(roomId, text);
      this.onMessageReceived(message);
    } catch (err) {
      this.error.set(hubErrorMessage(err, 'Message could not be sent.'));
    }
  }

  notifyTyping(): void {
    const roomId = this.activeRoomId();
    if (roomId && this.hub.state() === 'connected') {
      void this.hub.startTyping(roomId).catch(() => undefined);
    }
  }

  async startPrivateChat(userId: string): Promise<void> {
    try {
      const room = await firstValueFrom(this.roomsApi.startPrivateChat(userId));
      this.upsertRoom(room);
      await this.router.navigate(['/chat', room.id]);
    } catch {
      this.error.set('Could not start the chat.');
    }
  }

  async createGroup(name: string): Promise<boolean> {
    try {
      const room = await firstValueFrom(this.roomsApi.createGroup(name));
      this.upsertRoom(room);
      await this.router.navigate(['/chat', room.id]);
      return true;
    } catch (err) {
      this.error.set(
        err instanceof HttpErrorResponse
          ? (err.error?.detail ?? 'Could not create the group.')
          : 'Could not create the group.',
      );
      return false;
    }
  }

  async joinGroup(roomId: string): Promise<void> {
    try {
      const room = await firstValueFrom(this.roomsApi.join(roomId));
      this.upsertRoom(room);
      this.joinableGroups.update((list) => list.filter((g) => g.id !== roomId));
      await this.router.navigate(['/chat', room.id]);
    } catch {
      this.error.set('Could not join the group.');
    }
  }

  async leaveGroup(roomId: string): Promise<void> {
    try {
      await firstValueFrom(this.roomsApi.leave(roomId));
      this.rooms.update((list) => list.filter((r) => r.id !== roomId));
      this.markRead(roomId);
      await this.router.navigate(['/chat']);
    } catch {
      this.error.set('Could not leave the group.');
    }
  }

  clearError(): void {
    this.error.set(null);
  }

  // ----- Real-time events -----

  private listenToHub(destroyRef: DestroyRef): void {
    this.hub.messageReceived
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe((message) => this.onMessageReceived(message));

    this.hub.roomAdded.pipe(takeUntilDestroyed(destroyRef)).subscribe((room) => {
      this.upsertRoom(room);
      this.joinableGroups.update((list) => list.filter((g) => g.id !== room.id));
    });

    this.hub.membersChanged
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe(({ roomId, memberCount }) => {
        this.rooms.update((list) => list.map((r) => (r.id === roomId ? { ...r, memberCount } : r)));
        if (roomId === this.activeRoomId()) {
          void this.refreshActiveRoomDetails();
        }
      });

    this.hub.presenceChanged.pipe(takeUntilDestroyed(destroyRef)).subscribe((user) => {
      this.users.update((list) =>
        list.some((u) => u.id === user.id)
          ? list.map((u) => (u.id === user.id ? user : u))
          : [...list, user],
      );
      this.activeRoom.update((room) =>
        room ? { ...room, members: room.members.map((m) => (m.id === user.id ? user : m)) } : room,
      );
    });

    this.hub.userTyping
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe((typing) => this.onUserTyping(typing));

    this.hub.reconnected.pipe(takeUntilDestroyed(destroyRef)).subscribe(() => {
      // We may have missed events while offline: reload everything.
      void this.loadRooms();
      void this.loadUsers();
      const roomId = this.activeRoomId();
      if (roomId) {
        this.activeRoomId.set(null);
        void this.openRoom(roomId);
      }
    });
  }

  private onMessageReceived(message: Message): void {
    const room = this.rooms().find((r) => r.id === message.roomId);
    if (!room) {
      // A room we don't know yet (e.g. someone just started a chat with us).
      void this.loadRooms();
    } else {
      // Update the preview and move the room to the top of the list.
      const updated = { ...room, lastMessage: message, updatedAt: message.createdAt };
      this.rooms.update((list) => [updated, ...list.filter((r) => r.id !== room.id)]);
    }

    if (message.roomId === this.activeRoomId()) {
      // Ignore duplicates (we can get our own message twice: as the reply and as the event).
      if (!this.messages().some((m) => m.id === message.id)) {
        this.messages.update((list) => [...list, message]);
      }
    } else if (message.senderId !== this.currentUser()?.id) {
      this.unread.update((counts) => ({
        ...counts,
        [message.roomId]: (counts[message.roomId] ?? 0) + 1,
      }));
    }

    // The sender has stopped typing once their message arrives.
    this.clearTyping(`${message.roomId}:${message.senderId}`);
  }

  private onUserTyping(typing: Typing): void {
    const key = `${typing.roomId}:${typing.userId}`;
    this.typing.update((map) => ({ ...map, [key]: typing }));

    clearTimeout(this.typingTimers.get(key));
    this.typingTimers.set(
      key,
      setTimeout(() => this.clearTyping(key), TYPING_TIMEOUT_MS),
    );
  }

  private clearTyping(key: string): void {
    clearTimeout(this.typingTimers.get(key));
    this.typingTimers.delete(key);
    if (key in this.typing()) {
      this.typing.update(({ [key]: _, ...rest }) => rest);
    }
  }

  private async refreshActiveRoomDetails(): Promise<void> {
    const roomId = this.activeRoomId();
    if (!roomId) return;
    try {
      const details = await firstValueFrom(this.roomsApi.getDetails(roomId));
      if (this.activeRoomId() === roomId) this.activeRoom.set(details);
    } catch {
      // Ignore: we may have just left the room.
    }
  }

  private upsertRoom(room: Room): void {
    this.rooms.update((list) =>
      [room, ...list.filter((r) => r.id !== room.id)].sort(
        (a, b) => Date.parse(b.updatedAt) - Date.parse(a.updatedAt),
      ),
    );
  }

  private markRead(roomId: string): void {
    if (this.unread()[roomId]) {
      this.unread.update(({ [roomId]: _, ...rest }) => rest);
    }
  }

  private reset(): void {
    this.initialized = false;
    this.rooms.set([]);
    this.users.set([]);
    this.joinableGroups.set([]);
    this.activeRoomId.set(null);
    this.activeRoom.set(null);
    this.messages.set([]);
    this.unread.set({});
    this.typing.set({});
    this.error.set(null);
  }
}

/** SignalR error messages look like "An unexpected error occurred invoking 'X' on the server. HubException: <text>". */
function hubErrorMessage(err: unknown, fallback: string): string {
  const text = err instanceof Error ? err.message : '';
  const match = /HubException: (.*)$/.exec(text);
  return match?.[1] ?? fallback;
}
