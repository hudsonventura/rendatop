namespace server.Utils;

public interface IEmailNotification
{
    Task Notify(string toEmail, string title, string message, bool isHtml = false);
}
