# Receiving fact command integration checks

Run `dotnet run --project tests/ReceivingFacts/ReceivingFacts.csproj` on Windows
with SQL Server LocalDB. The executable creates and migrates a unique
`WmsReceivingFacts_*` database and deletes it in `finally`; application connection
strings are never loaded.

Checks independently seeded legacy fact receipt hashes, quantity/comment
isolation, explicit zero and unconfirmed null, invalid quantities and states,
same-request recovery, concurrent duplicate scans and distinct edits, replay after
closure, and absence of inventory effects. 1C source/sink doubles throw on any
call. Real SQL final-save barriers exercise optimistic concurrency and receipts.
Web retry UI is outside this console test.
