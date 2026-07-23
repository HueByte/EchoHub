# UpdateBackupService.cs

> **Source:** `src/EchoHub.Client/Services/UpdateBackupService.cs`

## Contents

- [BackupJsonContext](#backupjsoncontext)
- [UpdateBackupService](#updatebackupservice)
- [BackupInfo](#backupinfo)

---

## BackupJsonContext
> **File:** `src/EchoHub.Client/Services/UpdateBackupService.cs`  
> **Kind:** class

```csharp
[System.Text.Json.Serialization.JsonSerializable(typeof(BackupInfo))]
internal partial class BackupJsonContext : System.Text.Json.Serialization.JsonSerializerContext
```


BackupJsonContext is an internal partial class that provides the source-generated JSON serialization metadata for the BackupInfo type. It plugs into System.Text.Json’s source generator, enabling reflection-free serialization of BackupInfo when you configure a JsonSerializerOptions with this context.

## Remarks
This symbol acts as the concrete carrier of serialization metadata for BackupInfo within the JSON pipeline of EchoHub’s client. By centralizing the generated type information in a single context, it keeps serialization concerns isolated from business logic and allows the type to evolve without scattering attributes across multiple call sites. The pattern here—one generated context per data contract—supports predictable performance improvements while preserving a clean, minimal public surface.

## Notes
- The symbol is internal; it is intended for use within the containing assembly, not by external callers.
- The class is generated and partial; do not edit it by hand, as changes will be overwritten by the source generator.
- If you modify the BackupInfo shape, you must re-run code generation to keep the context in sync with the data contract.

---

## UpdateBackupService
> **File:** `src/EchoHub.Client/Services/UpdateBackupService.cs`  
> **Kind:** class

```csharp
public static class UpdateBackupService
```


Manages pre-update backups for the auto-updater and provides rollback support by snapshotting the running application prior to an update. Backups are stored under ~/.echohub/update-backup/ as backup.zip with a companion backup-info.json that records the version, application directory, and UTC timestamp. Use CreateBackup before applying an update; verify presence with BackupExists and inspect metadata with GetBackupInfo to drive a rollback if needed. The IsPostUpdate flag signals that a backup from a recent update exists, allowing startup logic to react accordingly.

---

## BackupInfo
> **File:** `src/EchoHub.Client/Services/UpdateBackupService.cs`  
> **Kind:** record

```csharp
public record BackupInfo(
    string Version,
    string AppDirectory,
    DateTimeOffset CreatedAt)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Version` | `string` | — |
| `AppDirectory` | `string` | — |
| `CreatedAt` | `DateTimeOffset` | — |


BackupInfo is a lightweight, value-like record that encapsulates metadata about a created backup. It carries the backup Version, the AppDirectory that was backed up, and the CreatedAt timestamp, enabling complete backup metadata to be passed around as a single unit.

## Remarks
BackupInfo, being a record with positional parameters, is immutable and benefits from value-based equality. This makes it ideal as a canonical data carrier when the UpdateBackupService reports or persists backup information, or when UI/logging layers need to compare or display backup entries.

## Example
```csharp
var backup = new BackupInfo(
    Version: "1.2.3",
    AppDirectory: "/opt/MyApp",
    CreatedAt: DateTimeOffset.UtcNow
);
```

## Notes
- Records provide structural equality; two instances with the same Version, AppDirectory, and CreatedAt compare as equal.
- CreatedAt uses DateTimeOffset to preserve offset information; prefer UTC (DateTimeOffset.UtcNow) when constructing backups to avoid timezone ambiguities.

---