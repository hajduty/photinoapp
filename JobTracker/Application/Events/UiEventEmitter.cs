using Photino.NET;
using System.Text.Json;
using System.Collections.Concurrent;

namespace JobTracker.Application.Events;

public sealed class UiEventEmitter : IUiEventEmitter
{
    private readonly object _lock = new();
    private readonly ConcurrentQueue<string> _pendingMessages = new();

    private PhotinoWindow? _window;

    public void RegisterWindow(PhotinoWindow window)
    {
        lock (_lock)
        {
            _window = window;
        }

        FlushPendingMessages();
    }

    public void Emit(string eventName)
    {
        Emit<object?>(eventName, null);
    }

    public void Emit<T>(string eventName, T data)
    {
        var message = JsonSerializer.Serialize(new
        {
            type = "event",
            name = eventName,
            data,
            timestamp = DateTime.UtcNow
        });

        PhotinoWindow? currentWindow;

        lock (_lock)
        {
            currentWindow = _window;
        }

        if (currentWindow is null)
        {
            _pendingMessages.Enqueue(message);
            return;
        }

        SendMessage(currentWindow, message);
    }

    private void FlushPendingMessages()
    {
        PhotinoWindow? currentWindow;

        lock (_lock)
        {
            currentWindow = _window;
        }

        if (currentWindow is null)
            return;

        while (_pendingMessages.TryDequeue(out var message))
        {
            SendMessage(currentWindow, message);
        }
    }

    private void SendMessage(PhotinoWindow window, string message)
    {
        try
        {
            window.SendWebMessage(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EventEmitter error: {ex.Message}");
        }
    }
}
