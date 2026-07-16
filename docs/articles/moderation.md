# Moderation & Roles

Every EchoHub server has a four-tier role hierarchy. Moderation is **strictly hierarchical**:
acting on another user requires outranking them — equal rank is never enough — and a few
invariants protect the server owner from lockouts.

## Roles

| Role | Rank | Users panel glyph | How it's granted |
| --- | --- | --- | --- |
| **Owner** | 3 | ★ | The first account ever registered on the server |
| **Admin** | 2 | ♦ | Assigned by the Owner |
| **Mod** | 1 | ❀ | Assigned by an Admin or the Owner |
| **Member** | 0 | — | Everyone else |

Assign roles with `/role <user> <admin|mod|member>`. Two rules apply:

- You can only assign roles **strictly below your own** — an Admin can promote to Mod but
  cannot create another Admin; only the Owner can.
- **Owner is not assignable and not demotable.** There is exactly one Owner (the first
  account), nobody can be promoted to it, and the Owner's role can't be changed.

## Actions

| Command | Minimum role | Effect |
| --- | --- | --- |
| `/kick <user> [reason]` | Mod | Disconnects the user. Not persistent — they can reconnect immediately. |
| `/ban <user> [reason]` | Admin | Persistent: flags the account banned and disconnects it. Banned accounts are rejected at login. |
| `/unban <user>` | Admin | Lifts a ban. |
| `/mute <user> [minutes]` | Mod | Blocks the user from sending messages or uploading files. Without a duration the mute is **indefinite**; with one it auto-expires (checked every ~15 seconds). |
| `/unmute <user>` | Mod | Lifts a mute early. |
| `/role <user> <role>` | Admin | Assign a role (see rules above). |
| `/nuke` | Mod | Deletes the **entire history of the current channel**, including all attachment files on disk. Channel-wide — no per-user check. |

Kick, ban, and mute all enforce the hierarchy: the target's role must be **strictly lower**
than yours. A Mod cannot kick another Mod; nobody can kick, ban, mute, or demote the Owner.

## Deleting messages

Deletion has its own, slightly different rule set:

- **Your own messages** — always deletable, whatever your role. Right-click a message →
  *Delete message*, or press `F6`, pick the message, and hit `Delete`.
- **Someone else's messages** — requires **Mod or higher** *and* strictly outranking the
  author. A Mod can delete a Member's message, but not another Mod's.

Deleting a message also purges its uploaded attachment blobs from the server's disk, and the
removal is broadcast live — the message disappears from everyone's chat immediately.

## How actions surface

Everyone in the channel sees moderation happen:

- **TUI clients** show system messages — *"alice was kicked (reason)"*, *"bob was banned"*,
  *"Channel history has been cleared by a moderator."* The kicked or banned user themselves
  gets a dialog with the reason, then the client disconnects.
- **IRC clients** get native protocol events: kicks arrive as a real `KICK` command, bans as
  a server `NOTICE`. (See the [IRC Gateway guide](irc-gateway.md).)

Muted users aren't announced; they simply receive *"You are muted and cannot send messages."*
when they try to speak.

## Design notes

- All checks run server-side in the moderation API — the client commands are conveniences,
  and the same rules bind IRC users and any direct API caller.
- Bans are account-level, not IP-level. A banned person can register a fresh account; pair
  bans with registration hygiene on public servers.
- In [end-to-end encrypted rooms](encrypted-rooms.md) moderation still works at the metadata
  level — messages can be deleted and users muted/kicked by identity — but no moderator can
  *read* the content, including the Owner.
