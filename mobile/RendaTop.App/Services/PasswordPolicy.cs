namespace RendaTop.App.Services;

public static class PasswordPolicy
{
    public const string Message =
        "A senha deve ter no minimo 9 caracteres, incluindo pelo menos 1 letra, 1 numero e 1 caractere especial.";

    public static string Validate(string password, string confirmation)
    {
        if (password != confirmation)
            return "As senhas nao conferem.";

        if (password.Length < 9 ||
            !password.Any(char.IsLetter) ||
            !password.Any(char.IsDigit) ||
            password.All(char.IsLetterOrDigit))
        {
            return Message;
        }

        return string.Empty;
    }
}
