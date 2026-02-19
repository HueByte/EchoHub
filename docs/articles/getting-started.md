# Getting Started

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run the Server

```bash
dotnet run --project src/EchoHub.Server
```

On first run, the server automatically:

1. Creates `appsettings.json` from the example config
2. Generates a secure JWT secret
3. Creates the SQLite database with a `#general` channel

## Run the Client

```bash
dotnet run --project src/EchoHub.Client
```

Connect to a server, register an account, and start chatting.

## Build from Source

```bash
dotnet build src/EchoHub.slnx
```
