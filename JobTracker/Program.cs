using JobTracker.Application.Features.JobTracker.Process;
using JobTracker.Application.Features.Notification;
using JobTracker.Application.Infrastructure.BackgroundJobs;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.Discord;
using JobTracker.Application.Infrastructure.Events;
using JobTracker.Application.Infrastructure.RPC;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Photino.NET;
using Photino.NET.Server;
using System.Drawing;
using System.Text;
using System.Text.Json;

namespace Photino.HelloPhotino.React;
//NOTE: To hide the console window, go to the project properties and change the Output Type to Windows Application.
// Or edit the .csproj file and change the <OutputType> tag from "WinExe" to "Exe".

class Program
{
    public static bool IsDebugMode = true;     //serve files from asp.net runtime

    [STAThread]
    static void Main(string[] args)
    {
        PhotinoServer
            .CreateStaticFileServer(args, out string baseUrl)
            .RunAsync();

        // Add services
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddDbContextFactory<AppDbContext>(options =>
                {
                    var dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
                    options.UseSqlite($"Data Source={dbPath}");
                });

                services.AddRpcSystem();
                services.AddSingleton<RpcDispatcher>();

                services.AddHostedService<JobTrackerWorker>();

                // Register event emitter after AddRpcSystem to avoid conflicts
                services.AddSingleton<IEventEmitter, EventEmitter>();

                // Register Discord webhook service
                services.AddSingleton<IDiscordWebhookService, DiscordWebhookService>();

                // Register domain event publisher
                services.AddSingleton<IEventPublisher, DomainEventPublisher>();

                // Register domain event handlers
                services.AddScoped<IEventHandler<JobsFoundEvent>, JobsFoundEventHandler>();
            })
            .Build();

        // DB init + seeding using HOST services
        using (var scope = host.Services.CreateScope())
        {
            var factory = scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<AppDbContext>>();

            using var db = factory.CreateDbContext();
            db.Database.EnsureCreated();
            SeedData.Initialize(factory);
        }

        // Start background workers (non-blocking)
        _ = host.StartAsync();

        // The appUrl is set to the local development server when in debug mode.
        // This helps with hot reloading and debugging.
        string appUrl = IsDebugMode ? "http://localhost:3000" : $"{baseUrl}/index.html";
        Console.WriteLine($"Serving React app at {appUrl}");

        // Window title declared here for visibility
        string windowTitle = "JobTracker V1";

        // Creating a new PhotinoWindow instance with the fluent API
        var window = new PhotinoWindow()
            .SetTitle(windowTitle)
            .SetUseOsDefaultSize(false)
            .SetSize(new Size(2048, 1024))
            // Resize to a percentage of the main monitor work area
            //.Resize(50, 50, "%")
            .SetUseOsDefaultSize(false)
            .SetSize(new Size(800, 600))
            // Center window in the middle of the screen
            .Center()
            // Users can resize windows by default.
            // Let's make this one fixed instead.
            .SetResizable(true)
            .SetWebSecurityEnabled(true)
            .RegisterCustomSchemeHandler("app", (object sender, string scheme, string url, out string contentType) =>
            {
                contentType = "text/javascript";
                return new MemoryStream(Encoding.UTF8.GetBytes(@"
                        (() =>{
                            window.setTimeout(() => {
                                alert(`🎉 Dynamically inserted JavaScript.`);
                            }, 1000);
                        })();
                    "));
            })
            // Most event handlers can be registered after the
            // PhotinoWindow was instantiated by calling a registration 
            // method like the following RegisterWebMessageReceivedHandler.
            // This could be added in the PhotinoWindowOptions if preferred.
            .RegisterWebMessageReceivedHandler(async (object sender, string message) =>
            {
                var window = (PhotinoWindow)sender;

                try
                {
                    var dispatcher = host.Services.GetRequiredService<RpcDispatcher>();

                    var responseJson = await dispatcher.DispatchAsync(message);

                    window.SendWebMessage(responseJson);
                }
                catch (Exception ex)
                {
                    // Last-resort error (dispatcher should usually handle errors itself)
                    window.SendWebMessage(JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = ex.Message
                    }));
                }
            })
            .Load(appUrl);
        
        var eventEmitter = host.Services.GetRequiredService<IEventEmitter>();
        eventEmitter.RegisterWindow(window);

        window.WaitForClose(); // Starts the application event loop
        host.StopAsync();
    }
}