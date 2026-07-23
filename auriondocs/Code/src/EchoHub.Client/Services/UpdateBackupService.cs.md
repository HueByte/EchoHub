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


Defines a source-generated JSON serialization context for `BackupInfo` by annotating the internal partial class ``BackupJsonContext`` with ``JsonSerializable(typeof(BackupInfo))``. This enables high-performance, reflection-free JSON serialization and deserialization via System.Text.Json's source generator when working with ``BackupInfo``.

## Remarks
By centralizing the JSON metadata in ``BackupJsonContext``, the codebase gains a single, version-stable contract for serializing ``BackupInfo``. The generated ```JsonTypeInfo<BackupInfo>``` exposed as ``BackupJsonContext.Default.BackupInfo`` is consumed by ``JsonSerializer`` overloads that accept type metadata, reducing runtime reflection and enabling better inlining and optimization. This scope-limited context also makes it straightforward to extend serialization support to additional related types by extending the same context without changing call-sites.


---

## UpdateBackupService
> **File:** `src/EchoHub.Client/Services/UpdateBackupService.cs`  
> **Kind:** class

```csharp
public static class UpdateBackupService
```


UpdateBackupService is a centralized helper that manages pre-update backups and rollback restoration for the auto-updater. It stores backups under the user profile in `~/.echohub/update-backup/` and exposes operations to create a snapshot, verify an existing backup, and read its metadata. Before applying an update, `CreateBackup()` snapshots the current application directory (via `AppContext.BaseDirectory`) into a ZIP named `backup.zip` and writes a `backup-info.json` containing the version, app directory, and timestamp. It skips log files to avoid locking issues, uses `CompressionLevel.Fastest` for speed, and annotates the backup with the current version from `UpdateChecker.CurrentVersion`. `BackupExists()` checks for the presence of both `backup.zip` and `backup-info.json`, while `GetBackupInfo()` reads and deserializes the metadata using `BackupJsonContext.Default.BackupInfo`. The `IsPostUpdate` flag signals that a post-update backup was produced and may influence rollback or recovery flow.

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


BackupInfo is a `record` that encapsulates the metadata for a backup produced by the application. It aggregates the `Version` string, the `AppDirectory` path where the backup resides, and the creation timestamp `CreatedAt` as a `DateTimeOffset`, providing a single, immutable value that callers can transport, compare, or display without reconstructing individual fields. Use this type whenever you need to pass around a complete snapshot of backup identity and location rather than scattering primitive values.

## Remarks
Because `BackupInfo` is a `record`, it provides value-based equality and immutability, so two backups with the same `Version`, `AppDirectory`, and `CreatedAt` compare as equal. This makes it ideal as a transport object across service boundaries and as a stable key or result in collections. It also supports deconstruction, enabling concise extraction of its three fields when needed.

## Notes
- This object is immutable; its properties are set at construction time and cannot be changed afterward.
- The `CreatedAt` value uses `DateTimeOffset` to preserve the exact point in time including offset, which is important for cross-system backups and logs.

---