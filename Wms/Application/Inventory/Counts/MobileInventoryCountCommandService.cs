using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Wms.Application.MobileCommands;
using Wms.Application.StockKeepingUnits;
using Wms.Common;
using Wms.Domain.Enums;

namespace Wms.Application.Inventory.Counts;

public sealed class MobileInventoryCountCommandService(
    MobileCommandExecutor mobileCommandExecutor,
    InventoryCountCommandService inventoryCountCommandService,
    StockKeepingUnitService stockKeepingUnitService)
{
    // Keep the persisted command type stable for receipts created by earlier versions.
    private const string StartCommand = "inventory-count.create";
    private const string IncrementCommand = "inventory-count.increment-sku";
    private const string SetQuantityCommand = "inventory-count.set-quantity";
    private const string SetSkuQuantityCommand = "inventory-count.set-sku-quantity";
    private const string RemoveItemCommand = "inventory-count.remove-item";
    private const string PostCommand = "inventory-count.post";
    private const string DeleteDraftCommand = "inventory-count.delete-draft";

    public Task<OperationResult<Guid>> StartAsync(
        Guid warehouseId,
        Guid storageLocationId,
        Guid clientRequestId,
        string userId,
        CancellationToken ct = default) =>
        mobileCommandExecutor.ExecuteAsync(
            StartCommand,
            clientRequestId,
            Hash(warehouseId, storageLocationId),
            userId,
            async (dbContext, token) =>
            {
                var existing = await dbContext.InventoryCounts
                    .AsNoTracking()
                    .Where(x => x.StorageLocationId == storageLocationId
                        && x.Status == InventoryCountStatus.Draft)
                    .Select(x => new { x.Id, x.WarehouseId })
                    .SingleOrDefaultAsync(token);
                if (existing is not null)
                {
                    return existing.WarehouseId == warehouseId
                        ? existing.Id
                        : OperationError.Invalid("Ячейка принадлежит другому складу.");
                }

                var result = await inventoryCountCommandService.StageCreateAsync(
                    dbContext,
                    warehouseId,
                    storageLocationId,
                    userId,
                    token);
                return result.IsSuccess ? result.Value!.Id : result.Error!;
            },
            ct);

    public Task<OperationResult<Guid>> IncrementSkuAsync(
        Guid inventoryCountId,
        string? barcode,
        Guid clientRequestId,
        string userId,
        CancellationToken ct = default) =>
        mobileCommandExecutor.ExecuteAsync(
            IncrementCommand,
            clientRequestId,
            MobileCommandExecutor.ComputeHash($"{inventoryCountId:N}|{barcode}"),
            userId,
            async (dbContext, token) =>
            {
                var skuResult = await stockKeepingUnitService.ResolveByBarcodeAsync(
                    dbContext,
                    barcode,
                    token);
                if (!skuResult.IsSuccess)
                    return skuResult.Error!;

                var result = await inventoryCountCommandService.StageIncrementSkuAsync(
                    dbContext,
                    inventoryCountId,
                    skuResult.Value!.Id,
                    userId,
                    token);
                return result.IsSuccess ? result.Value!.Id : result.Error!;
            },
            ct);

    public Task<OperationResult<Guid>> SetCountedQuantityAsync(
        Guid inventoryCountId,
        Guid itemId,
        decimal countedQuantity,
        Guid clientRequestId,
        string userId,
        CancellationToken ct = default) =>
        mobileCommandExecutor.ExecuteAsync(
            SetQuantityCommand,
            clientRequestId,
            MobileCommandExecutor.ComputeHash(string.Join(
                '|',
                inventoryCountId.ToString("N"),
                itemId.ToString("N"),
                countedQuantity.ToString("G29", CultureInfo.InvariantCulture))),
            userId,
            async (dbContext, token) =>
            {
                var result = await inventoryCountCommandService.StageSetCountedQuantityAsync(
                    dbContext,
                    inventoryCountId,
                    itemId,
                    countedQuantity,
                    userId,
                    token);
                return result.IsSuccess ? itemId : result.Error!;
            },
            ct);

    public Task<OperationResult<Guid>> RemoveUnexpectedItemAsync(
        Guid inventoryCountId,
        Guid itemId,
        Guid clientRequestId,
        string userId,
        CancellationToken ct = default) =>
        ExecuteDocumentActionAsync(
            RemoveItemCommand,
            inventoryCountId,
            itemId,
            clientRequestId,
            userId,
            (dbContext, token) => inventoryCountCommandService.StageRemoveUnexpectedItemAsync(
                dbContext,
                inventoryCountId,
                itemId,
                userId,
                token),
            ct);

    public Task<OperationResult<Guid>> SetSkuCountedQuantityAsync(
        Guid inventoryCountId,
        Guid stockKeepingUnitId,
        decimal countedQuantity,
        Guid clientRequestId,
        string userId,
        CancellationToken ct = default) =>
        mobileCommandExecutor.ExecuteAsync(
            SetSkuQuantityCommand,
            clientRequestId,
            MobileCommandExecutor.ComputeHash(string.Join(
                '|',
                inventoryCountId.ToString("N"),
                stockKeepingUnitId.ToString("N"),
                countedQuantity.ToString("G29", CultureInfo.InvariantCulture))),
            userId,
            async (dbContext, token) =>
            {
                var result = await inventoryCountCommandService.StageSetSkuCountedQuantityAsync(
                    dbContext,
                    inventoryCountId,
                    stockKeepingUnitId,
                    countedQuantity,
                    userId,
                    token);
                return result.IsSuccess ? result.Value!.Id : result.Error!;
            },
            ct);

    public Task<OperationResult<Guid>> PostAsync(
        Guid inventoryCountId,
        Guid clientRequestId,
        string userId,
        CancellationToken ct = default) =>
        ExecuteDocumentActionAsync(
            PostCommand,
            inventoryCountId,
            null,
            clientRequestId,
            userId,
            (dbContext, token) => inventoryCountCommandService.StagePostAsync(
                dbContext,
                inventoryCountId,
                userId,
                token),
            ct);

    public Task<OperationResult<Guid>> DeleteDraftAsync(
        Guid inventoryCountId,
        Guid clientRequestId,
        string userId,
        CancellationToken ct = default) =>
        ExecuteDocumentActionAsync(
            DeleteDraftCommand,
            inventoryCountId,
            null,
            clientRequestId,
            userId,
            (dbContext, token) => inventoryCountCommandService.StageDeleteDraftAsync(
                dbContext,
                inventoryCountId,
                userId,
                token),
            ct);

    private Task<OperationResult<Guid>> ExecuteDocumentActionAsync(
        string commandType,
        Guid inventoryCountId,
        Guid? itemId,
        Guid clientRequestId,
        string userId,
        Func<Data.ApplicationDbContext, CancellationToken, Task<OperationResult>> stageAction,
        CancellationToken ct) =>
        mobileCommandExecutor.ExecuteAsync(
            commandType,
            clientRequestId,
            itemId is Guid id ? Hash(inventoryCountId, id) : Hash(inventoryCountId),
            userId,
            async (dbContext, token) =>
            {
                var result = await stageAction(dbContext, token);
                return result.IsSuccess
                    ? itemId ?? inventoryCountId
                    : result.Error!;
            },
            ct);

    private static string Hash(params Guid[] ids) =>
        MobileCommandExecutor.ComputeHash(string.Join('|', ids.Select(x => x.ToString("N"))));

}
