using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.RequestObjects;

namespace tests;

public class CalculatorTests
{
    [Fact]
    public void PercentYear_CalculatesExpectedValues()
    {
        using var context = CreateContext();
        var calculator = (ICalculator)new Calculator_PERCENT_YEAR(context);
        var buyDate = new DateTime(2024, 1, 1);
        var sellDate = buyDate.AddDays(200);

        var request = new InvestmentRequest
        {
            title = "Prefixado",
            value = 1000m,
            date_buy = buyDate,
            index = IdexesType.PERCENT_YEAR,
            index_percent = 12m,
            taxes = true
        };

        var result = calculator.Generate(request, sellDate);

        Assert.Equal(6.459016m, result.effective_index_percent_brute, 6);
        Assert.Equal(64.590164m, result.profit_brute, 6);
        Assert.Equal(1064.590164m, result.value_brute, 6);
        Assert.Equal(20m, result.IR, 6);
        Assert.Equal(12.918033m, result.IR_value, 6);
        Assert.Equal(0m, result.IOF, 6);
        Assert.Equal(0m, result.IOF_value, 6);
        Assert.Equal(51.672131m, result.profit_liq, 6);
        Assert.Equal(1051.672131m, result.value_liq, 6);
    }

    [Fact]
    public void Cdi_CalculatesExpectedValues()
    {
        using var context = CreateContext();
        context.selics.Add(new Selic
        {
            date = new DateOnly(2024, 3, 1),
            value = 10m
        });
        context.SaveChanges();

        var calculator = (ICalculator)new Calculator_CDI(context);
        var buyDate = new DateTime(2024, 1, 1);
        var sellDate = buyDate.AddDays(200);

        var request = new InvestmentRequest
        {
            title = "CDB CDI",
            value = 1000m,
            date_buy = buyDate,
            index = IdexesType.CDI,
            index_percent = 110m,
            taxes = true
        };

        var result = calculator.Generate(request, sellDate);

        Assert.Equal(6.027397m, result.effective_index_percent_brute, 6);
        Assert.Equal(60.273973m, result.profit_brute, 6);
        Assert.Equal(1060.273973m, result.value_brute, 6);
        Assert.Equal(20m, result.IR, 6);
        Assert.Equal(12.054795m, result.IR_value, 6);
        Assert.Equal(0m, result.IOF, 6);
        Assert.Equal(0m, result.IOF_value, 6);
        Assert.Equal(48.219178m, result.profit_liq, 6);
        Assert.Equal(1048.219178m, result.value_liq, 6);
    }

    [Fact]
    public void IpcaMais_CalculatesExpectedValues()
    {
        using var context = CreateContext();

        //Moka as taxas IPCA apenas para conseguir rodar os testes
        context.ipcas.Add(new IPCA
        {
            date = new DateOnly(2024, 2, 1),
            value = 0.5m
        });
        context.SaveChanges();

        var calculator = (ICalculator)new Calculator_IPCA_MAIS(context);
        var buyDate = new DateTime(2024, 1, 1);
        var sellDate = new DateTime(2025, 1, 1);

        var request = new InvestmentRequest
        {
            title = "Tesouro IPCA+",
            value = 1000m,
            date_buy = buyDate,
            index = IdexesType.IPCA_MAIS,
            index_percent = 5m,
            taxes = true
        };

        var result = calculator.Generate(request, sellDate);

        Assert.Equal(11.08m, result.effective_index_percent_brute, 2);
        Assert.Equal(110.76m, result.profit_brute, 2);
        Assert.Equal(1110.76m, result.value_brute, 2);
        Assert.Equal(17.5m, result.IR, 2);
        Assert.Equal(19.38m, result.IR_value, 2);
        Assert.Equal(0m, result.IOF, 6);
        Assert.Equal(0m, result.IOF_value, 6);
        Assert.Equal(91.378996m, result.profit_liq, 6);
        Assert.Equal(1091.378996m, result.value_liq, 6);
    }

    private static Context CreateContext()
    {
        var options = new DbContextOptionsBuilder<Context>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new Context(options);
    }
}
