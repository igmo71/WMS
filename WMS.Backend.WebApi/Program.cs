using Serilog;
using System.Reflection;
using WMS.Backend.Application;
using WMS.Backend.Common;
using WMS.Backend.Domain.Models;
using WMS.Backend.Infrastructure;
using WMS.Backend.Infrastructure.Data;
using WMS.Backend.MessageBus;
using WMS.Backend.SignalRHub;
using WMS.Backend.WebApi.Endpoints;

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

                builder.Services.AddAuthorization();

                builder.Services.AddIdentityApiEndpoints<AppUser>()
                .AddEntityFrameworkStores<AppDbContext>();                

                builder.Services.AddProblemDetails();
                builder.Services.AddExceptionHandler<AppExceptionHandler>();

                builder.Services.AddOpenApi();

                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();
                                
                builder.Services.AddAppSignalR();

                var clientUrl = builder.Configuration.GetSection("ClientUrl").Get<string[]>()
                    ?? throw new InvalidOperationException("ClientUrl not found");
                builder.Services.AddCors(options =>
                {
                    options.AddDefaultPolicy(policy =>
                    {
                        policy
                            //.WithOrigins(clientUrl)
                            .AllowAnyOrigin() // TODO: Development only
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
                });

                builder.Services.AddAppMessageBus(builder.Configuration);
                builder.Services.AddAppRepositories(builder.Configuration);
                builder.Services.AddAppServices(builder.Configuration);

                // TODO: CorrelationId Не используется сейчас
                //builder.Services.AddSingleton<ICorrelationContext, CorrelationContext>();
                //builder.Services.AddTransient<CorrelationIdMiddleware>();

                // Настройка JSON-сериализации // TODO: Пока воздержимся
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

                // TODO: CorrelationId Не используется сейчас
                //app.UseMiddleware<CorrelationIdMiddleware>();

                app.UseSerilogRequestLogging();

                //app.UseHttpsRedirection();

                app.UseCors();

                app.UseAuthorization();

                app.UseDefaultFiles();
                app.UseStaticFiles();

                app.MapFallbackToFile("index.html"); // Для SPA

                app.MapIdentityApi<AppUser>();

                app.MapAppHub();

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
