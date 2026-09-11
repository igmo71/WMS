# Shipping rollback checks

Run `dotnet run --project tests/ShippingRollback/ShippingRollback.csproj` on
Windows with SQL Server LocalDB and the .NET 10 SDK installed.

The console suite creates a unique temporary database, applies real migrations,
checks model drift, and deletes the database in `finally`. It does not use the
application connection string. 1C ports throw if rollback accesses them; posted
fixtures use public picking completion with separate in-memory 1C doubles.

Coverage: receipt lookup before state, exact original reason hash, invalid
requests/reason/state, draft removal and audit reset, posted compensation and
preserved turnover history, lock/stock rejection without partial persistence,
same-request recovery, replay after a new picking cycle, concurrent identical
and distinct rollback, rollback racing draft editing, and shipped prohibition.

These checks do not automate the Web reason dialog or component retry UI.
