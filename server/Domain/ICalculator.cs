using server.RequestObjects;

namespace server.Domain;

public interface ICalculator
{
    List<Calculated> Calculate(InvestmentRequest request);

    Calculated Generate(InvestmentRequest request, DateTime sell);
}
