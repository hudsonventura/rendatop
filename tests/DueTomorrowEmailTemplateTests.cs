using server.BackgroundServices;
using server.Domain;
using server.Utils;

namespace tests;

public class DueTomorrowEmailTemplateTests
{
    [Fact]
    public void Build_ReturnsStyledHtmlWithInvestmentSummary()
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

        var summary = new DueTomorrowNotificationSummary(
            GrossProfit: 234.56m,
            IncomeTax: 45.67m,
            NetValue: 1423.45m);

        var html = DueTomorrowEmailTemplate.Build(user, investment, summary, "https://app.rendatop.test");

        Assert.Contains("<html", html);
        Assert.Contains("Vencimento amanha", html);
        Assert.Contains("Resumo do vencimento", html);
        Assert.Contains("CDB Liquidez", html);
        Assert.Contains("Banco Inter", html);
        Assert.Contains("Valor investido", html);
        Assert.Contains("Rendimento bruto", html);
        Assert.Contains("IR", html);
        Assert.Contains("Valor liquido", html);
        Assert.Contains("R$", html);
        Assert.Contains("#dc2626", html);
        Assert.Contains("#16a34a", html);
        Assert.Contains("https://app.rendatop.test/icon.png", html);
        Assert.Contains("Vencimento", html);
    }
}
