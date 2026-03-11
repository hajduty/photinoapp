using Photino.NET;
using System.Text.Json;

namespace JobTracker.Application.Events;

public sealed class UiEventEmitter : IUiEventEmitter
{
    private readonly object _lock = new();
    private PhotinoWindow? _window;
    private bool _windowAlive = false;

    public void RegisterWindow(PhotinoWindow window)
    {
        lock (_lock)
        {
            _window = window;
            _windowAlive = true;
        }
    }

    public void UnregisterWindow()
    {
        lock (_lock)
        {
            _windowAlive = false;
            _window = null;
        }
    }

    public void Emit(string eventName) => Emit<object?>(eventName, null);

    public void Emit<T>(string eventName, T data)
    {
        PhotinoWindow? currentWindow;
        lock (_lock)
        {
            if (!_windowAlive || _window == null) return;
            currentWindow = _window;
        }

        if (currentWindow is null) return;

        var message = JsonSerializer.Serialize(new
        {
            type = "event",
            name = eventName,
            data,
            timestamp = DateTime.UtcNow
        });

        SendMessage(currentWindow, message);
    }

    private void SendMessage(PhotinoWindow window, string message)
    {
        try
        {
            window.Invoke(() => window.SendWebMessage(message));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EventEmitter error: {ex.Message}");
        }
    }
}