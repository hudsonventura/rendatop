using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using api.domain;
using Microsoft.IdentityModel.Tokens;

namespace api.services;

/// <summary>
/// Serviço responsável por gerar e validar tokens JWT.
/// </summary>
public static class JwtService
{
    private static readonly string _secret = Environment.GetEnvironmentVariable("JWT_SECRET")
        ?? throw new InvalidOperationException("JWT_SECRET não configurado no .env");

    private static readonly int _expirationDays = 30;

    /// <summary>
    /// Gera um token JWT para o usuário autenticado.
    /// </summary>
    public static string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: "rendatop",
            audience: "rendatop",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_expirationDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Retorna a chave simétrica usada para validação de tokens.
    /// </summary>
    public static SymmetricSecurityKey GetSecurityKey()
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
    }

    /// <summary>
    /// Duração do token em dias.
    /// </summary>
    public static int ExpirationDays => _expirationDays;
}
