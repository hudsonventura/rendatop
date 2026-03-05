namespace server.Utils;

public interface INotification
{
    Task Notify(string title, string message);
}
