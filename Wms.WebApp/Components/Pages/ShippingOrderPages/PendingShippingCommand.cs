using Wms.Application.Commands;

namespace Wms.WebApp.Components.Pages.ShippingOrderPages;

// Retained only for this component's lifetime, including uncertain retries.
internal sealed record PendingShippingCommand<TInput>(TInput Input, CommandContext Context);
