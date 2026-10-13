import { DatePipe } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';

import { RoomDetails } from '../../../core/models/room.model';

/**
 * Title bar of the open chat: name, online status / members, and "Leave" for groups.
 */
@Component({
  selector: 'app-room-header',
  imports: [RouterLink, DatePipe],
  templateUrl: './room-header.html',
  styleUrl: './room-header.scss',
})
export class RoomHeader {
  readonly room = input<RoomDetails | null>(null);
  readonly currentUserId = input<string | null>(null);

  readonly left = output<string>();

  protected readonly otherUser = computed(() => {
    const room = this.room();
    return room?.type === 'private'
      ? (room.members.find((m) => m.id === room.otherUserId) ?? null)
      : null;
  });

  protected readonly onlineCount = computed(
    () => this.room()?.members.filter((m) => m.isOnline).length ?? 0,
  );

  protected readonly memberNames = computed(() =>
    (this.room()?.members ?? [])
      .map((m) => (m.id === this.currentUserId() ? 'You' : m.nickname))
      .join(', '),
  );

  protected leave(): void {
    const room = this.room();
    if (room && confirm(`Leave "${room.name}"?`)) {
      this.left.emit(room.id);
    }
  }
}
