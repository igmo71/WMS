using Serilog;
using System.Reflection;
using WMS.Backend.Application;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models;
using WMS.Backend.Infrastructure;
using WMS.Backend.Infrastructure.Data;
using WMS.Backend.MessageBus;
using WMS.Backend.WebApi.Endpoints;
using WMS.Backend.WebApi.Hubs;

namespace WMS.Backend.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppConfiguration.ConfigureSerilog();

            using var activityListener = AppConfiguration.ConfigureSerilogTrasing();

            var appName = Assembly.GetEntryAssembly()?.GetName().Name;
            try
            {
                Log.Information("Hello, {Name}! {Application} is Starting up...", Environment.UserName, appName);

                var builder = WebApplication.CreateBuilder(args);

                builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings)));

                builder.Services.AddSerilog();

                builder.Services.AddSignalR();

                builder.Services.AddAuthorization();

                builder.Services.AddIdentityApiEndpoints<AppUser>()
                .AddEntityFrameworkStores<AppDbContext>();

                // TODO: CorrelationId Не используется сейчас
                //builder.Services.AddSingleton<ICorrelationContext, CorrelationContext>();
                //builder.Services.AddTransient<CorrelationIdMiddleware>();

                builder.Services.AddProblemDetails();
                builder.Services.AddExceptionHandler<AppExceptionHandler>();

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

                app.UseExceptionHandler();
                //app.UseStatusCodePages();

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

                app.UseSerilogRequestLogging();

                // TODO: CorrelationId Не используется сейчас
                //app.UseMiddleware<CorrelationIdMiddleware>();

                //app.UseHttpsRedirection();

                app.UseAuthorization();

                app.MapIdentityApi<AppUser>();

                app.MapAppHubs();

                app.MapAppEndpoints();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "{Application} terminated unexpectedly", appName);
            }
            finally
            {
                Log.Information("{Application} is Shutting down...", appName);
                Log.CloseAndFlush();
            }
        }
    }
}
