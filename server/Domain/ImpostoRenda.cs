
using server.Utils;

namespace server.Domain;

/// <summary>
/// Imposto de Renda
/// </summary>
public class ImpostoRenda : ITax
{
    /// <summary>
    /// CDB paga imposto de renda?
    ///Até 180 dias de aplicação: alíquota de 22,5% de IR;
    ///Entre 181 e 360 dias: alíquota de 20%;
    ///Entre 361 e 720 dias: alíquota de 17,5%;
    ///Acima de 720 dias: alíquota de 15%.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="finish"></param>
    /// <returns></returns>
    public decimal GetTax(DateTime start, DateTime? finish)
    {
        if(finish is null){
            throw new ExpectedException("Uma data estimada de fim de investimento deve ser informada");
        }

        DateTime end = (DateTime)finish;

        var diff = end - start;
        if (diff.Days <= 180)
            return 22.5m;
        if (diff.Days > 180 && diff.Days <= 365)
            return 20m;
        if (diff.Days > 365 && diff.Days <= 730)
            return 17.5m;
        return 15m;
    }
    
}
