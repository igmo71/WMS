# Receiving command regression checks

Run on Windows with SQL Server LocalDB installed:

```powershell
dotnet run --project tests/ReceivingCommands/ReceivingCommands.csproj
```

The executable creates a uniquely named `WmsReceivingPilot_<GUID>` database on
`(localdb)\MSSQLLocalDB` and deletes only that database in `finally`. No application
connection string is used. 1C source and sink are test doubles; EF and SQL Server
are real, including migrations, constraints, concurrency tokens and transactions.

Checks cover schema drift, legacy receipt migration/replay, hash conflicts,
both completion location inputs, actual posting/turnover, checkpoint persistence
after failure, retry, concurrent receiving requests, receipt uniqueness races,
atomic rollback, known concurrency conflicts and unknown persistence exceptions.
Browser interaction and a live 1C installation are not exercised here.
