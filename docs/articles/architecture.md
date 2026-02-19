# Architecture

## Overview

EchoHub follows a decentralized model where each server is fully independent. There is no central authority or account federation. Users create one account per server.

## Components

### EchoHub.Core

Shared library containing:

- **Models**: `User`, `Channel`, `Message`, `RefreshToken`
- **DTOs**: Record types for API requests/responses
- **Contracts**: `IEchoHubClient` -- the strongly-typed SignalR client interface
- **Constants**: `ValidationConstants` (shared regex patterns), `HubConstants`

### EchoHub.Server

ASP.NET Core web application:

- **Controllers**: REST API endpoints for auth, channels, users, files, server info
- **Hubs**: SignalR `ChatHub` for real-time messaging
- **Auth**: JWT token service (15-min access tokens, 30-day refresh tokens)
- **Data**: EF Core with SQLite
- **Services**: Presence tracking, file storage, image-to-ASCII conversion

### EchoHub.Client

Terminal.Gui v2 TUI application:

- **UI**: Main window, dialogs, chat renderer with ANSI color support
- **Services**: API client with automatic token refresh, SignalR connection wrapper
- **Themes**: 6 built-in color themes
- **Config**: Client configuration management

## Communication

- REST API for authentication, profile management, channel CRUD, file uploads
- SignalR WebSocket for real-time messaging and presence updates
- JWT tokens passed via query string for SignalR authentication
