namespace JobTracker.Application.Events;

public interface IUiEventEmitter
{
    void Emit(string eventName);
    void Emit<T>(string eventName, T data);
    void RegisterWindow(Photino.NET.PhotinoWindow window);
}
