namespace server.Utils;

public interface IWhatsAppNotification
{
    Task Notify(string phone, string title, string message);
}
