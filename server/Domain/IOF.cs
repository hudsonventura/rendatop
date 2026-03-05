
namespace server.Domain;

public class IOF : ITax
{
    public decimal GetTax(DateTime start, DateTime? finish)
    {
        var days = (finish - start).Value.Days;
        if (days >= 30)
        {
            return 0;
        }
        return (decimal)(100 - (days * 3.3333333333333333m));
    }
}
