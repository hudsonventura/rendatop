using server.RequestObjects;

namespace server.Domain;

public class Calculator_CDI_MAIS : Calculator, ICalculator
{
    public Calculator_CDI_MAIS(Context context) : base(context) { }

    Calculated ICalculator.Generate(InvestmentRequest request, DateTime sell)
    {
        decimal IR = GetIR(request.taxes, request.date_buy, sell);
        decimal IOF = GetIOF(request.date_buy, sell);
        decimal selic_avg = GetSelic(request.date_buy, sell);

        int days = (sell - request.date_buy).Days;

        // CDI médio anual + spread adicional ao ano.
        decimal annual_rate = selic_avg + (request.index_percent / 100m);
        decimal effective_index_percent = annual_rate / 366 * (days - 3);

        decimal profit_brute = request.value * effective_index_percent;
        decimal profit_brute_iof = profit_brute * (1 - IOF);
        decimal profit_liq = profit_brute_iof * (1 - IR);

        return new Calculated()
        {
            effective_index_percent_brute = effective_index_percent * 100,
            profit_brute = profit_brute,
            value_brute = request.value + profit_brute,

            IR = IR * 100,
            IR_value = profit_brute_iof * IR,

            IOF = IOF * 100,
            IOF_value = profit_brute * IOF,

            profit_liq = profit_liq,
            value_liq = request.value + profit_liq
        };
    }
}
