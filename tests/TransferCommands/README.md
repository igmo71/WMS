# Transfer command integration checks

Run `dotnet run --project tests/TransferCommands/TransferCommands.csproj` on Windows
with SQL Server LocalDB installed. The executable creates a uniquely named
`WmsTransferCommands_*` database, migrates it, and deletes it in `finally`. It never
loads an application connection string.

Checks legacy Mobile receipt hashes independently, replay before mutable checks,
actual direct/transit posting and turnover, deletion replay, business rejection
without a receipt, retry after stock changes, and deterministic final-save races.
Races cover duplicate requests, exclusive transit acquisition and competing stock
withdrawals. No browser, device or 1C service is needed.
