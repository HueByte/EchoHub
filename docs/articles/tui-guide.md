# TUI Guide

Everything you can do in the EchoHub terminal client: keyboard shortcuts, mouse actions,
slash commands, themes, and the everyday behaviors (unread markers, auto-join, scrollback)
that make it feel like a proper IRC-era client with modern comforts.

## Layout

```text
┌ Menu bar ──────────────────────────────────────────────────┐
│ ┌ Channels ─┐ ┌ Messages ────────────────────┐ ┌ Users ──┐ │
│ │ #general 3│ │ 12:01 <alice> hi             │ │ ★ alice │ │
│ │ #dev      │ │ ── new messages ──           │ │ ❀ bob   │ │
│ │ #random*~ │ │ 12:04 <bob> anyone around?   │ │ carol   │ │
│ └───────────┘ └──────────────────────────────┘ │ d [irc] │ │
│ ┌ Message │ Enter=send │ Tab=complete │ … ────┐ └─────────┘ │
│ │ _                                           │             │
│ └─────────────────────────────────────────────┘             │
│ Status: Connected │ v0.2.14 │ alice │ Act: #dev             │
└─────────────────────────────────────────────────────────────┘
```

Channel list markers: `*` = password-protected, `~` = private (unlisted), plus unread counts
(orange when you were @mentioned). Users panel glyphs: `★` Owner, `♦` Admin, `❀` Mod,
`[irc]` for IRC-gateway-only users; status icons `●`/`○`/`◐`/`◌` for online/offline/away/dnd.

## Keyboard shortcuts

### In the message input

| Key | Action |
| --- | --- |
| `Enter` | Send the message (also sends staged attachments with the text as caption) |
| `Ctrl+N` | Insert a newline (multiline message) |
| `Tab` | Autocomplete a slash command (`/th` → `/theme`) |
| `Ctrl+V` (or `Ctrl+Y`) | Paste — copied files and images become attachments, text pastes normally ([details](messages-and-attachments.md)) |
| `Ctrl+C` / `Ctrl+X` | Copy / cut in the input |
| `Ctrl+W` | Delete the word left of the cursor |
| `Ctrl+K` | Open the search palette |
| `F6` | Move focus into the message list |

### In the message list (after `F6`)

| Key | Action |
| --- | --- |
| `↑` / `↓` | Select a message |
| `Enter` | Activate: play/download/save an attachment, open an `@mention`'s profile, join a `#channel`, or open the sender's profile |
| `Delete` / `Backspace` | Delete the selected message (with confirmation; [permission rules](moderation.md)) |
| `F6` | Return focus to the input |

### Anywhere

| Key | Action |
| --- | --- |
| `F2` | Toggle the users panel |
| `Ctrl+K` | Search palette |
| `Alt+Q` | Quit |

## The search palette (`Ctrl+K`)

A command-palette that searches **channels and app actions** — type to filter, `↓` to
navigate, `Enter` to jump. Actions include Connect, Disconnect, Logout, My Profile,
Set Status, Create/Delete Channel, Saved Servers, Toggle Users Panel, Check for Updates,
and Quit. `Ctrl+K` again closes it.

## Mouse

- **Right-click a message** for the context menu: save image / play audio / download file
  (depending on the attachment), *Mention @user*, *View profile*, *Copy text*,
  *Copy message ID*, *Delete message*.
- **Left-click a message** does the most useful thing for that line: attachments
  play/download/save, `@mentions` and the sender open profiles, `#channel` references join
  that channel.
- **Click a user** in the users panel to open their profile; **click a channel** to switch.

## Slash commands

Type `/help` in any channel for the full list. The highlights:

| Command | What it does |
| --- | --- |
| `/status <online\|away\|dnd\|invisible>` or `/status <message>` | Presence / status message |
| `/nick <name>`, `/color <#hex>`, `/avatar <url or path>` | Display name, nick color, avatar |
| `/theme <name>` | Switch theme |
| `/send`, `/clear`, `/size`, `/downloadpath` | Attachments — see [Messages & Attachments](messages-and-attachments.md) |
| `/join <channel> [password]`, `/leave`, `/topic <text>` | Channel membership and topic |
| `/passwd <old> <new>` | Rotate an encrypted room's passphrase |
| `/profile [user]`, `/users`, `/meta` | Profiles, online users, room info |
| `/kick`, `/ban`, `/mute`, `/role`, `/nuke`, … | [Moderation](moderation.md) |
| `/servers`, `/quit` | Saved servers, exit |

Emoji shortcodes (`:smile:` style) are replaced live as you type.

## Themes

14 built-in themes: **Default, Transparent, TransparentLight, Classic, Light, Hacker,
Solarized, Dracula, Monokai, Nord, Gruvbox, Ocean, HighContrast, RosePine** — switch from
the User menu or `/theme <name>`. The two *Transparent* themes use no background color at
all, so your terminal's own background (and any blur/acrylic) shows through.

You can add your own: drop a theme JSON into `~/.echohub/themes/` and it appears in the list
(names that collide with a built-in are skipped).

## Everyday behaviors

- **Unread markers** — a `── new messages ──` rule marks where you left off in each channel,
  irssi-style. Read positions are **persisted per server**, so the marker survives
  reconnects and restarts. The status bar's `Act:` segment lists channels with activity
  (orange when you were @mentioned), and day boundaries draw a date rule.
- **Auto-join** — connecting joins `#general` plus every channel you're a member of, so
  unread counts and mentions accumulate everywhere. Channels you `/leave` stay left, and
  password-protected or [encrypted rooms](encrypted-rooms.md) are never auto-prompted —
  join those explicitly. `#general` is the home channel and can't be left or deleted.
- **Scrollback** — history loads 100 messages at a time; scrolling to the top of a channel
  fetches the next page and keeps your position (no jump).
- **Drag & drop** — dropping a file onto the window stages it as an attachment.
