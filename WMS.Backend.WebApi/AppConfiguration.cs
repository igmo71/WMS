using Serilog;
using SerilogTracing;

namespace WMS.Backend.WebApi
{
    public class AppConfiguration
    {
        public static void ConfigureSerilog()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json")
               .AddJsonFile($"appsettings.{environment ?? "Production"}.json", true)
               .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }

        public static IDisposable ConfigureSerilogTrasing()
        {
            var activityListener = new ActivityListenerConfiguration()
                .Instrument.WithDefaultInstrumentation(withDefaults: true) // TODO: ???
                .Instrument.AspNetCoreRequests(opts => opts.IncomingTraceParent = IncomingTraceParent.Trust)
                .Instrument.SqlClientCommands()
                .TraceToSharedLogger();
            return activityListener;
        }
    }
}
