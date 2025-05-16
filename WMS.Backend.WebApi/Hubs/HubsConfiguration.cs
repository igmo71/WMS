namespace WMS.Backend.WebApi.Hubs
{
    public static class HubsConfiguration
    {
        public static void MapAppHubs(this IEndpointRouteBuilder routes)
        {
            routes.MapHub<OrderInHub>("/OrderInHub");
        }
    }
}
