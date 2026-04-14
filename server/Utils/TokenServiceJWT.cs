using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using server.Domain;

namespace server.Utils;

public class TokenServiceJWT : ITokenService
{

    private static string secret_key = Environment.GetEnvironmentVariable("VITE_JWT_KEY") ?? throw new ArgumentNullException("VITE_JWT_KEY");


    public string Generate(User user, string role)
    {
        var claims = new List<Claim>
        {
            new Claim("Name", user.name),
            new Claim("Email", user.email),
            new Claim("UserType", user.user_type.ToString()),
            new Claim(ClaimTypes.Role, user.user_type == UserType.Admin ? "Admin" : role)
        };

        if (user.user_type == UserType.Admin)
            claims.Add(new Claim("Sou admin", "verdade"));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = GetJwtSecurityToken(claims.ToArray(), creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string Renew(string token)
    {
        var principal = (ClaimsPrincipal) GetTokenData(token);

        var claims = principal.Identities.First().Claims.ToList();
        var newToken = GetJwtSecurityToken(claims.ToArray(), new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret_key)),
                SecurityAlgorithms.HmacSha256
            ));
        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(newToken);

    }

    public void Validate(string token)
    {
        GetTokenData(token);
    }
    
    public dynamic GetTokenData(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "SeuIssuer",
            ValidAudience = "SeuAudience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret_key))
        };
        return handler.ValidateToken(token, validationParams, out var validatedToken);
    }


    private JwtSecurityToken GetJwtSecurityToken(Claim[] claims, SigningCredentials? credentials = null){
        return new JwtSecurityToken(
            issuer: "SeuIssuer",
            audience: "SeuAudience",
            claims: claims,
            expires: DateTime.Now.AddDays(30),
            signingCredentials: credentials
        );
    }


}
