# Migrations note

The three files in `Migrations/` are hand-written and guarded with `IF OBJECT_ID / COL_LENGTH`, so they are safe on both new databases and ones
originally created by `EnsureCreated`. There is no `AppDbContextModelSnapshot`, so `dotnet ef migrations add` would try to re-create every table.

To switch to normal EF tooling (do this once, on a dev machine):

1. Back up the DB, then delete the `Migrations/` folder.
2. `dotnet ef migrations add Baseline` (generates a real snapshot).
3. On existing databases, insert the baseline row instead of running it:
   `INSERT INTO __EFMigrationsHistory(MigrationId, ProductVersion) VALUES ('<Baseline id>', '10.0.0')`.
4. Remove the `PendingModelChangesWarning` ignore in `Program.cs`.
