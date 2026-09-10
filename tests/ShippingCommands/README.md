# Shipping transition checks

Run on Windows with SQL Server LocalDB:

```powershell
dotnet run --project tests/ShippingCommands/ShippingCommands.csproj
```

Creates and deletes only its own `WmsShippingCommands_<GUID>` LocalDB database.
Uses real EF, migrations, SQL transactions and optimistic concurrency; 1C ports
are test doubles. Covers three existing receipt protocols, replay/conflicts,
full picking/shipping posting and turnover, checkpoint persistence on failure,
recovery from exact external targets, blocking assessments and concurrent starts.
Browser interaction and a live 1C installation are not exercised.
