# Client startup and configuration management

> How the client boots up and applies runtime configuration and settings.

This guide explains how the EchoHub client starts, where runtime configuration lives, and how configuration is loaded and persisted during the application's lifecycle. It focuses on the entrypoint's bootstrap responsibilities, the JSON-backed configuration helper, the in-memory configuration model, and the orchestrator that drives the UI and runtime behavior.

## src/EchoHub.Client/Program.cs
Client application entry point and bootstrapper.

The [Program](../Code/src/EchoHub.Client/Program.cs.md) file centralizes early startup responsibilities so the rest of the app can assume a prepared environment. Concretely, it performs CLI flag parsing, permission and environment defense (including best-effort Unix execute-bit handling inside try/catch), rollback and post-update housekeeping, PATH setup, and early logging configuration (noting that Serilog sinks may need explicit assembly inclusion for single-file publishes). After these defenses and default configuration provisioning run, Program hands control to the main UI/orchestration flow; per the topic relationships it relies on the [AppOrchestrator](../Code/src/EchoHub.Client/AppOrchestrator.cs.md) and the [ConfigManager](../Code/src/EchoHub.Client/Config/ConfigManager.cs.md) to load/apply configuration and run the runtime UI logic.

## src/EchoHub.Client/Config/ClientConfig.cs
Holds client runtime configuration.

The [ClientConfig](../Code/src/EchoHub.Client/Config/ClientConfig.cs.md) file defines the in-memory shape of user-configurable runtime settings the client uses at runtime. It contains the [AccountPreset](../Code/src/EchoHub.Client/Config/ClientConfig.cs.md) type, a small serializable container for DisplayName, Bio, and NicknameColor (all nullable to permit partial presets), and the [ClientConfig](../Code/src/EchoHub.Client/Config/ClientConfig.cs.md) aggregate that groups saved servers, the default account preset, active theme, and notification preferences. This model is the canonical boundary between persistence and UI: the [ConfigManager](../Code/src/EchoHub.Client/Config/ConfigManager.cs.md) reads/writes it to disk and the [AppOrchestrator](../Code/src/EchoHub.Client/AppOrchestrator.cs.md) consumes and updates it during user interaction.

## src/EchoHub.Client/Config/ConfigManager.cs
Reads and applies configuration at runtime.

The [ConfigManager](../Code/src/EchoHub.Client/Config/ConfigManager.cs.md) is a static helper that centralizes all JSON I/O and path handling for the client configuration stored under the user's home directory. It exposes Load and Save for the full [ClientConfig](../Code/src/EchoHub.Client/Config/ClientConfig.cs.md) plus convenience methods like SaveServer and RemoveServer; SaveServer updates or appends a server entry using a case-insensitive URL comparison, and RemoveServer also uses case-insensitive matching to avoid duplicates. The implementation performs directory creation and JSON formatting but treats persistence as best-effort: IO failures are swallowed so callers (for example, [Program](../Code/src/EchoHub.Client/Program.cs.md) at startup and [AppOrchestrator](../Code/src/EchoHub.Client/AppOrchestrator.cs.md) at runtime) get a resilient experience even when on-disk operations fail.

## src/EchoHub.Client/AppOrchestrator.cs
`AppOrchestrator` collaborates directly with `AccountPreset` and other members of this topic (4 dependency links).

The [AppOrchestrator](../Code/src/EchoHub.Client/AppOrchestrator.cs.md) implements the runtime orchestration and the bridge between UI commands and application behavior. It defines the constructor and lifetime methods such as RunAsync and Dispose, exposes the MainWindow, and wires a long list of command handlers (Connect/Disconnect, message submission, channel join/create/delete, user moderation commands, theme and profile changes, audio playback requests, update checks, rollback requests, file transfers, and more). It includes helpers like InvokeUI for marshaling actions to the UI thread and SaveServerToConfig to persist server entries. In practice, AppOrchestrator reads and mutates the [ClientConfig](../Code/src/EchoHub.Client/Config/ClientConfig.cs.md) model during these interactions and calls into the [ConfigManager](../Code/src/EchoHub.Client/Config/ConfigManager.cs.md) to persist changes; Program constructs or invokes the orchestrator after completing its bootstrap sequence.

How the pieces fit

Program performs defensive startup (environment, logging, housekeeping) and uses the [ConfigManager](../Code/src/EchoHub.Client/Config/ConfigManager.cs.md) to load the initial [ClientConfig](../Code/src/EchoHub.Client/Config/ClientConfig.cs.md). Once the environment and configuration are prepared, Program transfers control to the [AppOrchestrator](../Code/src/EchoHub.Client/AppOrchestrator.cs.md), which runs the main UI lifecycle, handles user commands, updates the in-memory ClientConfig (including [AccountPreset](../Code/src/EchoHub.Client/Config/ClientConfig.cs.md) and saved servers), and persists those changes back through the ConfigManager. Persistence is intentionally best-effort: IO errors are swallowed by ConfigManager so the orchestrator and UI remain responsive even if on-disk saves fail.

---
*Covers 4 of 4 source files identified for this topic.*

*Synthesised by Aurion on 2026-07-08 17:08:48 UTC*
