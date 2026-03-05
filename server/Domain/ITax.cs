namespace server.Domain;

public interface ITax
{
    decimal GetTax(DateTime start, DateTime? finish);
}
