# IRC Gateway

Every EchoHub server can expose a second door: a built-in **IRC gateway** that speaks the
classic IRC protocol on port 6667. Any standard IRC client — irssi, WeeChat, HexChat,
Halloy — can join the same channels as TUI users, see the same messages, and chat with the
same accounts. Under the hood both protocols call the same chat service, so a message sent
from IRC appears instantly in the TUI and vice versa (see [Architecture](architecture.md)).

## Enabling the gateway

The gateway is off by default. Enable it in `appsettings.json` (or `Irc__Enabled=true` as an
environment variable):

```json
{
  "Irc": {
    "Enabled": true,
    "Port": 6667,
    "TlsEnabled": false,
    "TlsPort": 6697,
    "TlsCertPath": "",
    "ServerName": "echohub",
    "Motd": "Welcome to EchoHub IRC Gateway!"
  }
}
```

The plaintext listener always starts on `Port`. The TLS listener on `TlsPort` starts only
when `TlsEnabled` is `true` **and** `TlsCertPath` points to a PKCS#12 (`.pfx`) certificate.
See the [configuration reference](configuration.md#irc-gateway) for every option.

## Connecting & authentication

Your IRC **nick is your EchoHub username** and your server password is your **account
password**. Two flows are supported:

```bash
# classic PASS/NICK/USER — most clients call this the "server password"
irssi -c chat.example.com -p 6667 -w <password> -n <username>
```

or **SASL PLAIN** (advertised via `CAP LS`), where the SASL username/password are the account
credentials.

A few things worth knowing:

- **Connecting auto-registers.** If the username doesn't exist yet, the gateway creates the
  account with that password (usernames: 3–50 chars of `a-z 0-9 _ -`; passwords: 6+ chars).
  The very first account ever created on a server becomes the **Owner**.
- Because of that, a typo'd password for an *existing* account fails with
  `Username is already taken` — the gateway tried to log in, couldn't, then tried to register
  the name. If you see that error, re-check your password.
- Connecting without a password is rejected: `Password required. Use PASS command or SASL PLAIN.`

## What maps to what

| IRC | EchoHub |
| --- | --- |
| `JOIN #room` | Join a channel (history is replayed on join) |
| `JOIN #room <key>` | Join a password-protected (`+k`) channel |
| `PART` / `QUIT` | Leave channel / disconnect |
| `LIST` | Public channels only (password-protected ones show a `[+k]` hint) |
| `TOPIC` | Read or set the channel topic (permission-checked) |
| `NAMES` / `WHO` | Online users in the channel |
| `WHOIS` | Profile: display name, channels, idle time, away status |
| `AWAY [message]` | Sets your EchoHub status to Away / back to Online |
| `MODE #room +k <key>` / `-k` | Set / clear the channel password |

Private (unlisted) channels don't appear in `LIST`, but members who know the exact name can
still `JOIN` them. Channels are not auto-created from IRC — create them from the TUI first.

## How messages look

- **Attachments** arrive as labeled link lines — `[Image: photo.png] https://…`,
  `♪ [Audio: song.mp3] https://…`, `[File: report.pdf] https://…` — and image attachments
  additionally render their **ASCII-art preview** using truecolor ANSI escapes, so a modern
  terminal IRC client shows actual picture previews.
- **Link embeds** are appended as `│`-prefixed text lines.
- Long messages are split at word boundaries into IRC-safe lines (~400 bytes each);
  incoming messages may be up to 2,000 characters like any EchoHub message.
- Your own messages aren't echoed back (standard IRC convention).
- Moderation actions surface natively: kicks arrive as `KICK`, bans and channel nukes as
  server `NOTICE`s.

## Limitations

The gateway bridges what IRC can express — and deliberately refuses what it can't:

- **No end-to-end encrypted rooms.** Joining an [encrypted room](encrypted-rooms.md) fails
  with *"Cannot join channel — end-to-end encrypted, use the EchoHub client."* Bridging one
  would require the server to hold the room key, breaking the zero-knowledge design.
- **No private messages.** `PRIVMSG` to a nick is rejected; EchoHub is channel-based.
- **Usernames, not display names.** Messages are attributed to the account username;
  a user's display name is visible via `WHOIS`/`WHO` (realname field).
- **No client features.** Uploading attachments, profiles, themes, and reactions to status
  changes are TUI-client features. Other users' status changes aren't pushed to IRC —
  discover them with `WHOIS`/`WHO`.

## How IRC users appear to TUI users

Users connected *only* through the gateway are tagged `[irc]` in the users panel — a hint
that they can't receive encrypted content or use client-side features. Someone connected
with both an IRC client and the TUI shows untagged.
