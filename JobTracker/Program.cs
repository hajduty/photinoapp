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
using System.Text.Json;

namespace Photino.HelloPhotino.React;

class Program
{
#if DEBUG
    public static bool IsDebugMode = true;
#else
    public static bool IsDebugMode = false;
#endif

    private static bool _shouldExit = false;
    private static bool _windowVisible = false;
    private static NotifyIcon? _notifyIcon;
    private static PhotinoWindow? _window;
    private static IHost? _host;
    private static string? _appUrl;
    private static IEventEmitter? _eventEmitter;
    private static RpcDispatcher? _dispatcher;


    [STAThread]
    static void Main(string[] args)
    {
        PhotinoServer
            .CreateStaticFileServer(args, out string baseUrl)
            .RunAsync();

        _host = Host.CreateDefaultBuilder(args)
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
                services.AddSingleton<IEventEmitter, EventEmitter>();
                services.AddSingleton<IDiscordWebhookService, DiscordWebhookService>();
                services.AddSingleton<IEventPublisher, DomainEventPublisher>();
                services.AddScoped<IEventHandler<JobsFoundEvent>, JobsFoundEventHandler>();
            })
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

        _eventEmitter = _host.Services.GetRequiredService<IEventEmitter>();
        _dispatcher = _host.Services.GetRequiredService<RpcDispatcher>();

        SetupTrayIcon();

        while (!_shouldExit)
        {
            if (!_windowVisible)
            {
                Thread.Sleep(100);
                Application.DoEvents();
                continue;
            }

            CreateAndShowWindow();
            
            _windowVisible = false;
            _window = null;
            //1WriteLine("Window closed, app running in tray");
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
        
        _window = new PhotinoWindow();
        
        if (iconExists)
        {
            _window.SetIconFile(fullIconPath);
        }
        
        _window.SetTitle("JobTracker V1 (RELEASE)")
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

        _eventEmitter?.RegisterWindow(_window);

        _window.WaitForClose();
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
            if (!_windowVisible)
            {
                _windowVisible = true;
            }
            else if (_window != null)
            {
                _window.SetMinimized(false);
            }
        });

        var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => ExitApp());

        contextMenu.Items.Add(showItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) =>
        {
            if (!_windowVisible)
            {
                _windowVisible = true;
            }
            else if (_window != null)
            {
                _window.SetMinimized(false);
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
        try { _window?.Close(); } catch { }
    }
}
