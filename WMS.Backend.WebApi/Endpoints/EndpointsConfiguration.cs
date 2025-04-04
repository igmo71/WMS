namespace WMS.Backend.WebApi.Endpoints
{
    public static class EndpointsConfiguration
    {
        public static void MapAppEndpoints(this IEndpointRouteBuilder routes)
        {
            routes.MapOrderEndpoints();
            routes.MapOrderInEndpoints();
            routes.MapProductEndpoints();
        }
    }
}
