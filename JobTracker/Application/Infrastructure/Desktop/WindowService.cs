/*namespace JobTracker.Application.Infrastructure.Desktop;

using Photino.NET;
using System.Drawing;

public class WindowService
{
    private PhotinoWindow? _window;
    private Point _dragOffset;

    public void RegisterWindow(PhotinoWindow window)
    {
        _window = window;
    }

    public void Minimize()
    {
        _window.SetMaximized(false);
    }

    public void Maximize()
    {
        _window.SetMaximized(true);
    }

    private bool _isMaximized = false;
    private Point _prevLocation;
    private Size _prevSize;
    public void ToggleMaximize()
    {
        if (_isMaximized)
        {
            _window.MoveTo(_prevLocation.X, _prevLocation.Y);
            _window.Size = _prevSize;
            _isMaximized = false;
        }
        else
        {
            _prevLocation = _window.Location;
            _prevSize = _window.Size;

            if (_window.Monitors.Count > 0)
            {
                var primary = _window.Monitors[0];

                _window.MoveTo(primary.MonitorArea.X, primary.MonitorArea.Y);
                _window.Size = new Size(primary.MonitorArea.Width, primary.MonitorArea.Height);
            }

            _isMaximized = true;
        }
    }

    public void Close()
    {
        _window.Close();
    }

    private Point _lastCursorPos;
    private Point _windowStartPos;

    public void StartDrag(int cursorScreenX, int cursorScreenY, bool isStart)
    {
        if (_window == null) return;

        if (isStart)
        {
            _lastCursorPos = new Point(cursorScreenX, cursorScreenY);
            _windowStartPos = _window.Location;
        }
        else
        {
            int deltaX = cursorScreenX - _lastCursorPos.X;
            int deltaY = cursorScreenY - _lastCursorPos.Y;

            int newX = _windowStartPos.X + deltaX;
            int newY = _windowStartPos.Y + deltaY;

            _window.MoveTo(newX, newY);
        }
    }

    public void Restore()
    {
        _window.MoveTo(100, 100);
        _window.Size = new Size(1200, 800);
    }
}
*/