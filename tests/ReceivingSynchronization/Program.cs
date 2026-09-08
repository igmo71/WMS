using Wms.Common;
using Wms.Domain;
using Wms.Domain.Enums;

// Run: dotnet run --project tests/ReceivingSynchronization
var now = DateTimeOffset.UtcNow;
var source = new ReceivingOrderImportSnapshot(
    Guid.NewGuid(), false, true, "Regression", now.UtcDateTime, Guid.NewGuid(),
    null, ReceivingOrderStatus.ReadyForReceiving, default,
    WarehouseOperation.VendorReceipt, BusinessOperation.VendorPurchase,
    Guid.NewGuid(), default, Guid.NewGuid(), null,
    Enumerable.Range(1, 5).Select(line => new ReceivingOrderItemImportSnapshot(
        line, Guid.NewGuid(), 18m, 18m, "")).ToArray());
var created = ReceivingOrder.Create(source, now);
if (!created.IsSuccess) throw new Exception(created.Error!.Message);
var order = created.Value!;
Require(order.SetReceivingLocation(Guid.NewGuid()));
Require(order.SetInReceiving(now, "regression"));
foreach (var item in order.Items)
    Require(order.UpdateItemFact(item.LineNumber, item.LineNumber == 5 ? 15m : 18m,
        item.LineNumber == 5 ? "Недовезли?" : null));

var target = source with
{
    Status = ReceivingOrderStatus.Received,
    Items = order.Items.Select(item => new ReceivingOrderItemImportSnapshot(
        item.LineNumber, item.StockKeepingUnitId, item.FactQuantity!.Value,
        item.FactQuantity.Value, item.Comment ?? "")).ToArray()
};
Expect(ReceivingOrderSynchronizationComparer.CompareReceivedTarget(order, target), null);
Require(order.SetReceived(now, "regression"));
Expect(ReceivingOrderSynchronizationComparer.Compare(order, target), null);

foreach (string? comment in new string?[] { null, "", "Другой комментарий", "Недовезли? " })
{
    var assessment = ReceivingOrderSynchronizationComparer.Compare(order, target with
    {
        Items = target.Items.Select(item => item.LineNumber == 5
            ? item with { Comment = comment } : item).ToArray()
    });
    Expect(assessment, null);
    if (assessment.Fingerprint != ReceivingOrderSynchronizationComparer.Compare(order, target).Fingerprint)
        throw new Exception("Line comments must not affect the fingerprint.");
}

Expect(ReceivingOrderSynchronizationComparer.Compare(order, target with
{
    Items = target.Items.Select(item => item.LineNumber == 1
        ? item with { Comment = "Неожиданный комментарий" } : item).ToArray()
}), null);
Expect(ReceivingOrderSynchronizationComparer.Compare(order, target with
{
    Items = target.Items.Select(item => item.LineNumber == 5
        ? item with { Quantity = 18m } : item).ToArray()
}), "items[5].quantity");

Require(order.Reconcile(target, now));
if (order.SynchronizationLevel != OrderSynchronizationLevel.Synchronized)
    throw new Exception("Reconciliation must clear the assessment.");
Console.WriteLine("PASS: shortage 15/18, comments do not block, retry target, received order, quantity differences block, reconciliation.");

static void Require(OperationResult result)
{
    if (!result.IsSuccess) throw new Exception(result.Error!.Message);
}

static void Expect(OrderSynchronizationAssessment assessment, string? blockedField)
{
    if (blockedField is null
        ? assessment.Level != OrderSynchronizationLevel.Synchronized
        : assessment.Level != OrderSynchronizationLevel.Blocking
            || assessment.Differences.Count != 1
            || assessment.Differences[0].FieldCode != blockedField)
        throw new Exception($"Unexpected assessment: {string.Join(", ", assessment.Differences.Select(x => x.FieldCode))}");
}
