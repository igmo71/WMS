# Putaway command integration checks

Run `dotnet run --project tests/PutawayCommands/PutawayCommands.csproj` on Windows
with SQL Server LocalDB. A unique `WmsPutawayCommands_*` database is created,
migrated and deleted in `finally`; application connection strings are not loaded.

Checks old Mobile receipt types/hashes, add/update/delete replay (even after
movement deletion), expected order ownership, split allocation, no stock changes
from drafts, completion rejection for destination locks and insufficient stock,
same-request recovery and atomic once-only posting. Final-save barriers exercise
duplicate start/add/complete, distinct allocation conflicts and edit/delete races.
No 1C service or browser/device UI is used.
