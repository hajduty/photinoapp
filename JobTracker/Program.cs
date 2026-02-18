using JobTracker.Application.Events;
using JobTracker.Application.Features.JobSearch.LoadJobs.Scraper;
using JobTracker.Application.Features.JobTracker;
using JobTracker.Application.Features.Notification;
using JobTracker.Application.Features.SemanticSearch;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.Discord;
using JobTracker.Application.Infrastructure.RPC;
using JobTracker.Application.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Photino.NET;
using Photino.NET.Server;
using System.Text.Json;

namespace Photino.HelloPhotino.React;

class Program
{
#if DEBUG
    public static bool IsDebugMode = true;
#else
    public static bool IsDebugMode = false;
#endif

    private static volatile bool _shouldExit = false;
    private static volatile bool _windowVisible = false;
    private static readonly object _windowLock = new object();
    private static NotifyIcon? _notifyIcon;
    private static PhotinoWindow? _window;
    private static IHost? _host;
    private static string? _appUrl;
    private static IUiEventEmitter? _eventEmitter;
    private static RpcDispatcher? _dispatcher;

    [STAThread]
    static void Main(string[] args)
    {
        PhotinoServer
            .CreateStaticFileServer(args, out string baseUrl)
            .RunAsync();

        _host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((Action<IServiceCollection>)(services =>
            {
                services.AddDbContextFactory<AppDbContext>(options =>
                {
                    var dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
                    options.UseSqlite($"Data Source={dbPath}");
                    options.LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Warning);
                });

                services.AddRpcSystem();
                services.AddSingleton<RpcDispatcher>();
                ServiceCollectionHostedServiceExtensions.AddHostedService<BackgroundWorker>(services);
                services.AddSingleton<IUiEventEmitter, UiEventEmitter>();
                services.AddSingleton<IDiscordWebhookService, DiscordWebhookService>();
                services.AddSingleton<IEventPublisher, DomainEventPublisher>();
                services.AddSingleton<JobTechScraper>();
                services.AddSingleton<ScrapeService>();
                services.AddSingleton<TrackerService>();
                services.AddSingleton<EmbeddingService>();
                services.AddSingleton<OllamaService>();

                services.AddScoped<IEventHandler<JobsFoundEvent>, JobsFoundEventHandler>();
                services.AddScoped<IEventHandler<EmbeddingsCancelled>, EmbeddingsCancelledHandler>();
                services.AddScoped<IEventHandler<EmbeddingsFinished>, EmbeddingsFinishedHandler>();
                services.AddScoped<IEventHandler<EmbeddingsStarted>, EmbeddingsStartedHandler>();
                services.AddScoped<IEventHandler<EmbeddingsProgress>, EmbeddingsProgressHandler>();
            }))
            .Build();

        using (var scope = _host.Services.CreateScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = factory.CreateDbContext();
            db.Database.EnsureCreated();
            SeedData.Initialize(factory);
        }

        _host.StartAsync().Wait();

        _appUrl = IsDebugMode ? "http://localhost:3000" : $"{baseUrl}/index.html";
        //Console.WriteLine($"Serving React app at {_appUrl}");

        _eventEmitter = _host.Services.GetRequiredService<IUiEventEmitter>();
        _dispatcher = _host.Services.GetRequiredService<RpcDispatcher>();

        SetupTrayIcon();

        while (!_shouldExit)
        {
            bool shouldCreateWindow;
            lock (_windowLock)
            {
                shouldCreateWindow = _windowVisible && _window == null;
            }

            if (!shouldCreateWindow)
            {
                Thread.Sleep(100);
                Application.DoEvents();
                continue;
            }

            CreateAndShowWindow();

            lock (_windowLock)
            {
                _windowVisible = false;
                _window = null;
            }
            //WriteLine("Window closed, app running in tray");
        }

        _notifyIcon?.Dispose();
        _host.StopAsync().Wait();
    }

    private static void CreateAndShowWindow()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        var fullIconPath = Path.GetFullPath(iconPath);
        var iconExists = File.Exists(fullIconPath);

        //Console.WriteLine($"Icon exists: {iconExists}, Path: {fullIconPath}");

        var window = new PhotinoWindow();

        if (iconExists)
        {
            window.SetIconFile(fullIconPath);
        }

        window.SetTitle("JobTracker V1 (RELEASE)")
            .SetUseOsDefaultSize(false)
            .SetSize(new Size(1200, 800))
            .Center()
            .SetResizable(true)
            .SetWebSecurityEnabled(true)
            .SetContextMenuEnabled(IsDebugMode)
            .SetDevToolsEnabled(IsDebugMode)
            .RegisterWebMessageReceivedHandler(async (sender, message) =>
            {
                var win = (PhotinoWindow)sender;
                try
                {
                    var responseJson = await _dispatcher!.DispatchAsync(message);
                    win.SendWebMessage(responseJson);
                }
                catch (Exception ex)
                {
                    win.SendWebMessage(JsonSerializer.Serialize(new { success = false, error = ex.Message }));
                }
            })
            .Load(_appUrl!);

        lock (_windowLock)
        {
            _window = window;
        }

        _eventEmitter?.RegisterWindow(window);

        try
        {
            window.WaitForClose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Window error: {ex.Message}");
        }
    }

    private static void SetupTrayIcon()
    {
        Icon trayIcon;
        var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (File.Exists(iconPath))
        {
            trayIcon = new Icon(iconPath);
        }
        else
        {
            trayIcon = SystemIcons.Application;
        }

        _notifyIcon = new NotifyIcon
        {
            Icon = trayIcon,
            Text = "JobTracker",
            Visible = true
        };

        var contextMenu = new ContextMenuStrip();

        var showItem = new ToolStripMenuItem("Show", null, (s, e) =>
        {
            lock (_windowLock)
            {
                if (_window != null)
                {
                    // Window exists, just restore it
                    try
                    {
                        _window.SetMinimized(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error showing window: {ex.Message}");
                        // Window is dead, create a new one
                        _windowVisible = true;
                    }
                }
                else if (!_windowVisible)
                {
                    // No window exists and none is being created, create one
                    _windowVisible = true;
                }
                // else: window is already being created, do nothing
            }
        });

        var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => ExitApp());

        contextMenu.Items.Add(showItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) =>
        {
            lock (_windowLock)
            {
                if (_window != null)
                {
                    // Window exists, just restore it
                    try
                    {
                        _window.SetMinimized(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error showing window: {ex.Message}");
                        // Window is dead, create a new one
                        _windowVisible = true;
                    }
                }
                else if (!_windowVisible)
                {
                    // No window exists and none is being created, create one
                    _windowVisible = true;
                }
                // else: window is already being created, do nothing
            }
        };

        // Start with window visible
        _windowVisible = true;
    }

    private static void ExitApp()
    {
        //Console.WriteLine("Exit requested");
        _shouldExit = true;
        _notifyIcon?.Dispose();

        lock (_windowLock)
        {
            try
            {
                _window?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing window: {ex.Message}");
            }
        }
    }
}