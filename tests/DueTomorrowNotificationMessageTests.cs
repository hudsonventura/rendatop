using server.BackgroundServices;
using server.Domain;

namespace tests;

public class DueTomorrowNotificationMessageTests
{
    [Fact]
    public void BuildMessage_IncludesGrossProfitIncomeTaxAndNetValue()
    {
        var user = new User
        {
            name = "Hudson Ventura",
            email = "hudsonventura@gmail.com",
            password = "secret"
        };

        var investment = new Investment
        {
            owner = user,
            title = "CDB Liquidez",
            bank = new Bank { Name = "Banco Inter" },
            value = 1234.56m,
            due_date = new DateTime(2026, 4, 12, 13, 30, 0, DateTimeKind.Utc)
        };

        var summary = new DueTomorrowNotificationSummary(234.56m, 45.67m, 1423.45m);

        var message = DueTomorrowNotificationBackgroundService.BuildMessage(user, investment, summary);

        Assert.Contains("Rendimento bruto", message);
        Assert.Contains("IR:", message);
        Assert.Contains("Valor líquido: R$", message);
        Assert.DoesNotContain("<b>", message);
        Assert.DoesNotContain("*R$", message);
    }

    [Fact]
    public void BuildTelegramMessage_UsesHtmlBoldForNetValue()
    {
        var user = new User
        {
            name = "Hudson Ventura",
            email = "hudsonventura@gmail.com",
            password = "secret"
        };

        var investment = new Investment
        {
            owner = user,
            title = "CDB Liquidez",
            bank = new Bank { Name = "Banco Inter" },
            value = 1234.56m,
            due_date = new DateTime(2026, 4, 12, 13, 30, 0, DateTimeKind.Utc)
        };

        var summary = new DueTomorrowNotificationSummary(234.56m, 45.67m, 1423.45m);

        var message = DueTomorrowNotificationBackgroundService.BuildTelegramMessage(user, investment, summary);

        Assert.Contains("Rendimento bruto", message);
        Assert.Contains("IR:", message);
        Assert.Contains("Valor líquido: <b>R$", message);
    }

    [Fact]
    public void BuildWhatsAppMessage_UsesMarkdownBoldForNetValue()
    {
        var user = new User
        {
            name = "Hudson Ventura",
            email = "hudsonventura@gmail.com",
            password = "secret"
        };

        var investment = new Investment
        {
            owner = user,
            title = "CDB Liquidez",
            bank = new Bank { Name = "Banco Inter" },
            value = 1234.56m,
            due_date = new DateTime(2026, 4, 12, 13, 30, 0, DateTimeKind.Utc)
        };

        var summary = new DueTomorrowNotificationSummary(234.56m, 45.67m, 1423.45m);

        var message = DueTomorrowNotificationBackgroundService.BuildWhatsAppMessage(user, investment, summary);

        Assert.Contains("Rendimento bruto", message);
        Assert.Contains("IR:", message);
        Assert.Contains("Valor líquido: *R$", message);
    }
}
