using server.Domain;
using server.Utils;

namespace tests;

public class InvestmentTypeDetectorTests
{
    [Theory]
    [InlineData("CDB Banco XPTO", InvestmentType.CDB)]
    [InlineData("RDB Banco XPTO", InvestmentType.RDB)]
    [InlineData("LCI de Testes", InvestmentType.LCI)]
    [InlineData("LCA Banco XPTO", InvestmentType.LCA)]
    [InlineData("Teste Debêntures XPTO", InvestmentType.Debentures)]
    [InlineData("RCI Banco XPTO", InvestmentType.RCI)]
    [InlineData("RCA Banco XPTO", InvestmentType.RCA)]
    [InlineData("Tesouro Seclic XPTO", InvestmentType.Tesouro)]
    public void Detect_ReturnsExpectedInvestmentType(string title, InvestmentType expectedType)
    {
        var detectedType = InvestmentTypeDetector.Detect(title);

        Assert.Equal(expectedType, detectedType);
    }
}
