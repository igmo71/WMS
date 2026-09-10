using Wms.Application.Commands;

namespace Wms.WebApp.Components.Pages.ReceivingOrderPages;

// Retained only for this component's lifetime, including uncertain retries.
internal sealed record PendingReceivingCommand<TCommand>(TCommand Command, CommandContext Context);
