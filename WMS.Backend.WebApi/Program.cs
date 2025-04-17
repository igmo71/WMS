using Serilog;
using WMS.Backend.Application;
using WMS.Backend.Common;
using WMS.Backend.Infrastructure;
using WMS.Backend.MessageBus;
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

                builder.Services.Configure<AppSettings>(
                    builder.Configuration.GetSection(nameof(AppSettings)));

                builder.Services.AddSerilog();

                builder.Services.AddAuthorization();

                // TODO: CorrelationId Не используется сейчас
                //builder.Services.AddSingleton<ICorrelationContext, CorrelationContext>();
                //builder.Services.AddTransient<CorrelationIdMiddleware>();


                builder.Services.AddOpenApi();

                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                builder.Services.AddAppMessageBus(builder.Configuration);
                builder.Services.AddAppRepositories(builder.Configuration);
                builder.Services.AddAppServices(builder.Configuration);

                // Настройка JSON-сериализации
                // TODO: Пока воздержимся
                //builder.Services.Configure<JsonOptions>(options =>
                //{
                //    options.SerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                //    options.SerializerOptions.MaxDepth = 64;
                //    options.SerializerOptions.WriteIndented = false;
                //    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                //    options.SerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic);
                //});

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

                // TODO: CorrelationId Не используется сейчас
                //app.UseMiddleware<CorrelationIdMiddleware>();

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
