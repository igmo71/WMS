using Serilog.Context;
using WMS.Backend.Common;

namespace WMS.Backend.WebApi.Middleware
{
    // TODO: CorrelationId Не используется сейчас
    public class CorrelationIdMiddleware(ICorrelationContext correlationContext) : IMiddleware
    {
        private readonly ICorrelationContext _correlationContext = correlationContext;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var correlationId = context.Request.Headers.TryGetValue(AppSettings.CORRELATION_HEADER, out var headerValue)
                ? headerValue.ToString()
                : Guid.NewGuid().ToString();

            _correlationContext.CorrelationId = correlationId;

            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(AppSettings.CORRELATION_HEADER))
                {
                    context.Response.Headers[AppSettings.CORRELATION_HEADER] = correlationId;
                }
                return Task.CompletedTask;
            });

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await next(context);
            }
        }
    }
}
