using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WMS.Backend.Application.Abstractions.Hubs;

namespace WMS.Backend.SignalRHub
{
    public static class SignalRConfiguration
    {
        public static IServiceCollection AddAppSignalR(this IServiceCollection services)
        {
            services.AddSignalR();

            services.AddScoped<IAppHubService, AppHubService>();

            return services;
        }

        public static void MapAppHub(this IEndpointRouteBuilder routes)
        {
            routes.MapHub<AppHub>("/hub/OrderIn");
        }
    }
}
