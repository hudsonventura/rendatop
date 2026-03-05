using server.Domain;

namespace server.Utils;

public interface ITokenService
{
    string Generate(User user, string role);

    string Renew(string token);

    void Validate(string token);

    dynamic GetTokenData(string token);
}