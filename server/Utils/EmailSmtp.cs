using System.Net;
using System.Net.Mail;

namespace server.Utils;

public class EmailSmtp : IEmailNotification
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _enableSsl;

    public EmailSmtp(
        string? host,
        string? port,
        string? username,
        string? password,
        string? fromEmail,
        string? fromName,
        string? enableSsl)
    {
        _host = (host ?? string.Empty).Trim();
        _port = int.TryParse(port, out var parsedPort) ? parsedPort : 587;
        _username = (username ?? string.Empty).Trim();
        _password = password ?? string.Empty;
        _fromEmail = (fromEmail ?? string.Empty).Trim();
        _fromName = (fromName ?? string.Empty).Trim();
        _enableSsl = string.Equals(enableSsl, "true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task Notify(string toEmail, string title, string message)
    {
        if (string.IsNullOrWhiteSpace(_host) || string.IsNullOrWhiteSpace(_fromEmail))
            throw new Exception("Configuração SMTP incompleta. Defina SMTP_HOST e SMTP_FROM_EMAIL.");

        var destination = (toEmail ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(destination))
            throw new Exception("Email de destino não informado.");

        using var mail = new MailMessage
        {
            From = string.IsNullOrWhiteSpace(_fromName)
                ? new MailAddress(_fromEmail)
                : new MailAddress(_fromEmail, _fromName),
            Subject = title,
            Body = message,
            IsBodyHtml = false
        };
        mail.To.Add(destination);

        using var smtp = new SmtpClient(_host, _port)
        {
            EnableSsl = _enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(_username))
            smtp.Credentials = new NetworkCredential(_username, _password);

        await smtp.SendMailAsync(mail);
    }
}
