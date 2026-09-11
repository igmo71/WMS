# Picking command integration checks

Run `dotnet run --project tests/PickingCommands/PickingCommands.csproj` on Windows
with SQL Server LocalDB. The executable creates/migrates a unique
`WmsPickingCommands_*` database and deletes it in `finally`; it never reads an
application connection string.

Checks independently seeded Mobile hashes, add/update/delete replay, facts
recalculated from split drafts, expected-order ownership, bounds/stock/locks,
rejection without effects and same-request recovery. Final-save barriers exercise
duplicate add/delete, distinct allocations and edit/delete races. Drafts do not
post inventory. ShippingCommands separately exercises actual picking completion
and shipping using these shared mutations. Browser/device interaction is not
covered by this console test.
