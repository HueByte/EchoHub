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


Declares a System.Text.Json source-generated context for serializing and deserializing the BackupInfo type. This internal partial class, annotated with JsonSerializable(typeof(BackupInfo)), enables the JSON serializer to generate type-specific code at compile time for BackupInfo, avoiding reflection at runtime.

## Remarks
By centralizing JSON metadata in a dedicated JsonSerializerContext, this symbol improves serialization performance and consistency across all code paths that touch BackupInfo. The internal partial class pattern and the generated Default context mean you typically use the generated context (for example, via the Default instance) rather than instantiating the type directly.

## Notes
- The class is internal; its serialization metadata is intended for assembly-internal use via the generated Default context, not via direct construction.
- If you modify the BackupInfo type, you must re-run the source generator to keep the context in sync with the type's shape.

---

## UpdateBackupService
> **File:** `src/EchoHub.Client/Services/UpdateBackupService.cs`  
> **Kind:** class

```csharp
public static class UpdateBackupService
```


UpdateBackupService provides a centralized mechanism to capture a snapshot of the application before an update, enabling rollback if the update fails. It creates a ZIP backup of the current application directory at the user's home under ~/.echohub/update-backup/ and records metadata in backup-info.json, skipping log files to avoid locking and performance issues.

## Remarks
By encapsulating the backup workflow, this class isolates the concerns of locating the backup directory, excluding volatile logs, and serializing metadata. It depends on AppContext.BaseDirectory and the current version from UpdateChecker, ensuring that the backup represents the exact state of the running application prior to update. The IsPostUpdate flag provides a lightweight signal that a post-update backup is available, allowing callers to decide whether a restoration path should be considered after startup.

## Notes
- Some files may be skipped during backup (logs, locked, or inaccessible files), so the archive may not be a perfect byte-for-byte copy.
- CreateBackup deletes any existing backup directory before creating a new one; if deletion fails due to locks or permissions, remnants may remain and affect the new backup.
- The backup location is per-user under the home directory; in restricted environments this path may be inaccessible, causing backup creation to fail.

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


BackupInfo is an immutable, value-based data container that captures the essential metadata produced when a backup is created by UpdateBackupService. It aggregates the backup Version, the AppDirectory where the backup resides, and the CreatedAt timestamp, enabling easy transport, comparison, and persistence of backup metadata across system boundaries.

## Remarks
By being a C# record, BackupInfo benefits from value-based equality and concise construction, making it ideal for passing around a cohesive snapshot of a backup's identity. Its immutability helps guard against accidental changes to backup metadata after creation, which is important for auditing, caching, and reconciliation tasks.

## Example
```csharp
var info = new BackupInfo(
    Version: "1.2.3",
    AppDirectory: "/var/app/backups/backup-20240601",
    CreatedAt: DateTimeOffset.UtcNow);
```

## Notes
- Immutable: properties are effectively init-only; once constructed, the BackupInfo instance cannot be mutated.
- AppDirectory is a plain string; no path validation is performed by this type.
- CreatedAt uses DateTimeOffset to preserve the wall-clock offset; ensure UTC when comparing across systems.

---