namespace server.Utils;

public static class CpfUtility
{
    public static string Normalize(string? cpf)
    {
        return new string((cpf ?? string.Empty).Where(char.IsDigit).ToArray());
    }

    public static bool IsValid(string? cpf)
    {
        var digits = Normalize(cpf);
        if (digits.Length != 11)
            return false;

        if (digits.Distinct().Count() == 1)
            return false;

        var firstDigit = CalculateDigit(digits[..9], 10);
        var secondDigit = CalculateDigit(digits[..10], 11);

        return digits[9] == firstDigit && digits[10] == secondDigit;
    }

    public static string NormalizeOrThrow(string? cpf)
    {
        var digits = Normalize(cpf);
        if (!IsValid(digits))
            throw new ExpectedException("CPF inválido.");

        return digits;
    }

    private static char CalculateDigit(string baseDigits, int factor)
    {
        var sum = 0;
        foreach (var digit in baseDigits)
        {
            sum += (digit - '0') * factor--;
        }

        var remainder = (sum * 10) % 11;
        return (remainder == 10 ? 0 : remainder).ToString()[0];
    }
}
