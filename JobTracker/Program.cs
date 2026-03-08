using JobTracker.Application.Events;
using JobTracker.Application.Features.JobSearch.LoadJobs.Scraper;
using JobTracker.Application.Features.JobTracker;
using JobTracker.Application.Features.Notification;
using JobTracker.Application.Features.SemanticSearch;
using JobTracker.Application.Infrastructure.Data;
using JobTracker.Application.Infrastructure.Discord;
using JobTracker.Application.Infrastructure.RPC;
using JobTracker.Application.Infrastructure.Services;
using JobTracker.Embeddings.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Photino.NET;
using Photino.NET.Server;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Photino.HelloPhotino.React;

class Program
{
#if DEBUG
    public static bool IsDebugMode = true;
#else
    public static bool IsDebugMode = false;
#endif

    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private static volatile bool _shouldExit = false;
    private static volatile bool _windowHidden = false;

    private static NotifyIcon? _notifyIcon;
    private static ToolStripMenuItem? _showHideItem;
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
                services.AddSingleton<BertService>();
                services.AddSingleton<JinaEmbeddingService>();
                services.AddSingleton<JobTechScraper>();
                services.AddSingleton<ScrapeService>();
                services.AddSingleton<TrackerService>();
                services.AddSingleton<EmbeddingProcessor>();
                services.AddSingleton<SentenceClassifierService>();

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
            var jinaService = scope.ServiceProvider.GetRequiredService<JinaEmbeddingService>();

            using var db = factory.CreateDbContext();
            db.Database.EnsureCreated();
            SeedData.Initialize(factory, jinaService);
        }

        _host.StartAsync().Wait();

        _appUrl = IsDebugMode ? "http://localhost:3000" : $"{baseUrl}/index.html";
        _eventEmitter = _host.Services.GetRequiredService<IUiEventEmitter>();
        _dispatcher = _host.Services.GetRequiredService<RpcDispatcher>();

        if (IsWindows)
            RunWindows();
        else
            RunCrossPlatform();

        _host.StopAsync().Wait();
    }

    private static void RunCrossPlatform()
    {
        CreateWindow();
        _window!.WaitForClose();
    }

    private static void RunWindows()
    {
        SetupTrayIcon();
        CreateWindow();
        _window!.WaitForClose();
        _notifyIcon?.Dispose();
    }

    private static void CreateWindow()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");

        _window = new PhotinoWindow();

        if (File.Exists(iconPath))
            _window.SetIconFile(iconPath);

        _window
            .SetTitle("JobTracker V1 (RELEASE)")
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

        if (IsWindows)
        {
            _window.WindowClosing += (sender, args) =>
            {
                if (_shouldExit)
                    return false; // allow real destruction

                HideWindow();
                return true; // cancel destruction
            };
        }

        _eventEmitter?.RegisterWindow(_window);
    }

    private static void HideWindow()
    {
        if (_window == null) return;
        _windowHidden = true;
        ShowWindow(_window.WindowHandle, SW_HIDE);
        UpdateTrayMenuText();
    }

    private static void ShowWindow()
    {
        if (_window == null) return;
        _windowHidden = false;
        ShowWindow(_window.WindowHandle, SW_SHOW);
        SetForegroundWindow(_window.WindowHandle);
        UpdateTrayMenuText();
    }

    private static void UpdateTrayMenuText()
    {
        if (_showHideItem == null) return;
        _showHideItem.Text = _windowHidden ? "Show" : "Hide";
    }

    private static void ToggleWindowVisibility()
    {
        if (_windowHidden) ShowWindow();
        else HideWindow();
    }

    private static void SetupTrayIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        Icon trayIcon = File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application;

        _showHideItem = new ToolStripMenuItem("Hide");
        _showHideItem.Click += (s, e) => ToggleWindowVisibility();

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => ExitApp();

        var contextMenu = new ContextMenuStrip
        {
            Renderer = new ToolStripSystemRenderer()
        };
        contextMenu.Items.Add(_showHideItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = trayIcon,
            Text = "JobTracker",
            Visible = true,
            ContextMenuStrip = contextMenu
        };

        _notifyIcon.DoubleClick += (s, e) => ToggleWindowVisibility();
    }

    private static void ExitApp()
    {
        _shouldExit = true;
        _notifyIcon?.Dispose();

        try { _window?.Close(); }
        catch (Exception ex) { Console.WriteLine($"Error closing window: {ex.Message}"); }
    }
}