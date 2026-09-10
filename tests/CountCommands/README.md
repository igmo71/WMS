# Inventory count command integration checks

Run `dotnet run --project tests/CountCommands/CountCommands.csproj` on Windows
with SQL Server LocalDB. The executable creates and migrates a uniquely named
`WmsCountCommands_*` database and deletes it in `finally`; it never loads an
application connection string.

Checks all seven legacy Mobile receipt hashes independently, replay before
mutable lookups (including removed barcode/rows/documents), changed inputs,
snapshot and lock ownership, once-only scans, absolute quantities, zero,
unexpected row deletion, posting positive/negative differences, rejected posting
and retry, stock drift and manual locks. Deterministic final-save barriers cover
simultaneous same-key start/scan/post and distinct-key start/edit conflicts.
No browser, Android device or 1C service is used.
