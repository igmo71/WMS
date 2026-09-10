using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Wms.Application.Persistence;
using Wms.Common;
using Wms.Data;

namespace Wms.Application.Commands;

public sealed class CommandExecutor(
    IDbContextFactory<ApplicationDbContext> dbContextFactory)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(
        string commandType,
        Guid requestId,
        string requestHash,
        string userId,
        Func<ApplicationDbContext, CancellationToken, Task<OperationResult<Guid>>> operation,
        CancellationToken ct)
    {
        if (requestId == Guid.Empty)
            return OperationError.Invalid("Идентификатор запроса обязателен.");
        if (string.IsNullOrWhiteSpace(userId))
            return OperationError.Invalid("Пользователь команды не определён.");

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        var existingReceipt = await FindReceiptAsync(
            dbContext,
            userId,
            commandType,
            requestId,
            ct);
        if (existingReceipt is not null)
            return ResolveReceipt(existingReceipt, requestHash);

        var result = await operation(dbContext, ct);
        if (!result.IsSuccess)
            return result.Error!;

        dbContext.CommandReceipts.Add(new CommandReceipt
        {
            UserId = userId,
            CommandType = commandType,
            RequestId = requestId,
            RequestHash = requestHash,
            ResultResourceId = result.Value,
            CompletedAtUtc = DateTimeOffset.UtcNow
        });

        try
        {
            await dbContext.SaveChangesAsync(ct);
            return result.Value;
        }
        catch (DbUpdateException exception)
        {
            await using var retryContext = await dbContextFactory.CreateDbContextAsync(ct);
            var winningReceipt = await FindReceiptAsync(
                retryContext,
                userId,
                commandType,
                requestId,
                ct);
            if (winningReceipt is not null)
                return ResolveReceipt(winningReceipt, requestHash);
            if (PersistenceConflictClassifier.TryClassify(exception, out var error))
                return error;
            throw;
        }
    }

    public static string ComputeHash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static Task<CommandReceipt?> FindReceiptAsync(
        ApplicationDbContext dbContext,
        string userId,
        string commandType,
        Guid requestId,
        CancellationToken ct) =>
        dbContext.CommandReceipts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId
                && x.CommandType == commandType
                && x.RequestId == requestId,
                ct);

    private static OperationResult<Guid> ResolveReceipt(
        CommandReceipt receipt,
        string requestHash) =>
        receipt.RequestHash == requestHash
            ? receipt.ResultResourceId
            : OperationError.Conflict("Этот идентификатор запроса уже использован с другими данными.");
}
