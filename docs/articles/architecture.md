# Architecture

## Overview

EchoHub follows a decentralized model where each server is fully independent. There is no central authority or account federation. Users create one account per server.

The server exposes two protocol interfaces to the same chat backend:

```text
IRC Client ──► TCP :6667 ──► IrcGateway ──┐
                                           ├──► ChatService ──► DB + PresenceTracker
TUI Client ──► WebSocket ──► ChatHub ─────┘
```

Both protocols call into a shared `IChatService` for business logic. Events fan out to all registered `IChatBroadcaster` implementations (SignalR and IRC).

## Components

### EchoHub.Core

Shared library containing:

- **Models**: `User`, `Channel`, `Message`, `RefreshToken`
- **DTOs**: Record types for API requests/responses
- **Contracts**: `IChatService` (protocol-agnostic chat operations), `IChatBroadcaster` (event fan-out interface), `IEchoHubClient` (SignalR client interface)
- **Constants**: `ValidationConstants` (shared regex patterns), `HubConstants`

### EchoHub.Server

ASP.NET Core web application:

- **Controllers**: REST API endpoints for auth, channels, users, files, server info
- **Hubs**: SignalR `ChatHub` -- thin adapter delegating to `IChatService`
- **Auth**: JWT token service (15-min access tokens, 30-day refresh tokens)
- **Data**: EF Core with SQLite
- **Services**: `ChatService` (core business logic), `SignalRBroadcaster`, presence tracking, file storage, image-to-ASCII conversion

### EchoHub.Server.Irc

IRC protocol gateway (separate project for clean separation of concerns):

- **IrcGatewayService**: `BackgroundService` with TCP listener on configured port(s), optional TLS
- **IrcCommandHandler**: Per-client IRC command dispatch -- handles `CAP`/`SASL`, `NICK`/`USER`/`PASS`, `JOIN`/`PART`/`PRIVMSG`/`QUIT`, `NAMES`/`TOPIC`/`WHO`/`WHOIS`/`AWAY`/`LIST`/`MODE`/`MOTD`
- **IrcBroadcaster**: `IChatBroadcaster` implementation that formats chat events as IRC protocol lines, with echo suppression (IRC convention)
- **IrcMessageFormatter**: Converts `MessageDto` to IRC `PRIVMSG` lines -- splits long text at word boundaries (~400 byte chunks), sends images as ASCII art line-by-line

IRC users authenticate with existing EchoHub accounts via `PASS`/`NICK`/`USER` or SASL PLAIN (BCrypt verification against the database).

### EchoHub.Client

Terminal.Gui v2 TUI application:

- **UI**: Main window, dialogs, chat renderer with ANSI color support
- **Services**: API client with automatic token refresh, SignalR connection wrapper
- **Themes**: 13 built-in color themes
- **Config**: Client configuration management

## Communication

- **REST API** for authentication, profile management, channel CRUD, file uploads
- **SignalR WebSocket** for real-time messaging and presence updates (TUI client)
- **IRC TCP** for real-time messaging via standard IRC protocol (IRC clients)
- JWT tokens passed via query string for SignalR authentication
- IRC authentication via PASS/SASL PLAIN against BCrypt password hashes

## Broadcaster Pattern

The `IChatBroadcaster` interface allows multiple protocols to receive chat events:

- **SignalRBroadcaster**: Wraps `IHubContext<ChatHub>`, filters out IRC connections
- **IrcBroadcaster**: Iterates live IRC connections in a channel, formats events as IRC protocol lines

Both are registered in DI and called by `ChatService` when events occur. This means a message sent from an IRC client appears in the TUI client, and vice versa.
