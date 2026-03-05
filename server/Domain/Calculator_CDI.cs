using server.RequestObjects;

namespace server.Domain;

public class Calculator_CDI : Calculator, ICalculator
{
    public Calculator_CDI(Context context) : base(context) { }



    Calculated ICalculator.Generate(InvestmentRequest request, DateTime sell)
    {
        decimal IR = GetIR(request.taxes, request.date_buy, sell);
        decimal IOF = GetIOF(request.date_buy, sell);


        decimal selic_avg = GetSelic(request.date_buy, sell);

        //percentual de rendimento bruto: selic_avg é a taxa anual média, dividida por 365 para obter a taxa diária
        decimal effective_index_percent = selic_avg / 365 * (sell - request.date_buy).Days * request.index_percent / 100;

        //rendimento bruto (sem impostos)
        decimal profit_brute = request.value * effective_index_percent;

        //redimento bruto retirado o IOF
        decimal profit_brute_iof = profit_brute * (1 - IOF);

        //rendimento liquido, descontado IOF e IR
        decimal profit_liq = profit_brute_iof * (1 - IR);


        return new Calculated()
        {
            effective_index_percent_brute = effective_index_percent * 100,
            profit_brute = profit_brute,
            value_brute = request.value + profit_brute,

            IR = IR * 100,
            IR_value = profit_brute_iof * IR, //calcula-se o IR sobre o valor bruto já descontado o IOF

            IOF = IOF * 100,
            IOF_value = profit_brute * IOF,

            profit_liq = profit_liq,
            value_liq = request.value + profit_liq

        };

    }


}
