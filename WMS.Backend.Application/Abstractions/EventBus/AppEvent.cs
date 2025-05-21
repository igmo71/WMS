namespace WMS.Backend.Application.Abstractions.EventBus
{
    public record CreatedEvent<T>(T Value) : IAppEvent;

    public record UpdatedEvent<T>(T Value) : IAppEvent;

    public record DeletedEvent(Guid Id) : IAppEvent;
}
