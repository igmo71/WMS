using Serilog;
using WMS.Backend.Application;
using WMS.Backend.Infrastructure;
using WMS.Backend.WebApi.Endpoints;

namespace WMS.Backend.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppConfiguration.ConfigureSerilog();

            using var activityListener = AppConfiguration.ConfigureSerilogTrasing();

            try
            {
                Log.Information("Hello, {Name}! App is Starting up...", Environment.UserName);

                var builder = WebApplication.CreateBuilder(args);

                builder.Services.AddSerilog();

                builder.Services.AddAuthorization();

                builder.Services.AddOpenApi();

                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                builder.Services.AddAppRepositories(builder.Configuration);

                builder.Services.AddAppServices(builder.Configuration);

                var app = builder.Build();

                //if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                    app.UseSwagger();
                    app.UseSwaggerUI();
                    //app.UseSwaggerUI(options =>
                    //{
                    //    options.SwaggerEndpoint("/openapi/v1.json", "OpenAPI V1");
                    //});
                }

                app.UseHttpsRedirection();

                app.UseAuthorization();

                app.MapAppEndpoints();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
