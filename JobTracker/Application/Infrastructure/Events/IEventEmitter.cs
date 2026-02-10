namespace JobTracker.Application.Infrastructure.Events;

public interface IEventEmitter
{
    void Emit(string eventName);
    void Emit<T>(string eventName, T data);
    void RegisterWindow(Photino.NET.PhotinoWindow window);
}
