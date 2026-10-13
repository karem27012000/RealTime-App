import { DatePipe } from '@angular/common';
import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { Room } from '../../../core/models/room.model';
import { User } from '../../../core/models/user.model';
import { ConnectionState } from '../../../core/services/chat-hub.service';

type Tab = 'chats' | 'people' | 'groups';

/**
 * Left panel with three tabs: my chats, people (start a private chat) and groups (create / join).
 * Presentational: data in, events out.
 */
@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, DatePipe, FormsModule],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss',
})
export class Sidebar {
  readonly currentUser = input<User | null>(null);
  readonly rooms = input.required<Room[]>();
  readonly users = input.required<User[]>();
  readonly joinableGroups = input.required<Room[]>();
  readonly activeRoomId = input<string | null>(null);
  readonly unread = input<Record<string, number>>({});
  readonly onlineUserIds = input<Set<string>>(new Set());
  readonly connectionState = input<ConnectionState>('disconnected');

  readonly userSelected = output<string>();
  readonly groupCreated = output<string>();
  readonly groupJoined = output<string>();
  readonly groupsTabOpened = output<void>();
  readonly loggedOut = output<void>();

  protected readonly tab = signal<Tab>('chats');
  protected readonly newGroupName = signal('');

  protected selectTab(tab: Tab): void {
    this.tab.set(tab);
    if (tab === 'groups') {
      this.groupsTabOpened.emit();
    }
  }

  protected createGroup(): void {
    const name = this.newGroupName().trim();
    if (name) {
      this.groupCreated.emit(name);
      this.newGroupName.set('');
      this.tab.set('chats');
    }
  }

  protected openPrivateChat(user: User): void {
    this.userSelected.emit(user.id);
    this.tab.set('chats');
  }

  protected joinGroup(room: Room): void {
    this.groupJoined.emit(room.id);
    this.tab.set('chats');
  }

  protected preview(room: Room): string {
    const last = room.lastMessage;
    if (!last) {
      return room.type === 'group' ? `${room.memberCount} members` : 'No messages yet';
    }
    const sender = last.senderId === this.currentUser()?.id ? 'You' : last.senderNickname;
    return room.type === 'group' || sender === 'You' ? `${sender}: ${last.content}` : last.content;
  }

  protected isOnline(room: Room): boolean {
    return room.otherUserId !== null && this.onlineUserIds().has(room.otherUserId);
  }
}
