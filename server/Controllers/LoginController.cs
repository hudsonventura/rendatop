using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Domain;
using server.Utils;
using StackExchange.Redis;

namespace server.Controllers;

[ApiController]
public class LoginController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly Context _context;
    private readonly IDatabase _redis;



    public LoginController(Context context, IConnectionMultiplexer muxer_redis)
    {
        _context = context;
        _redis = muxer_redis.GetDatabase();
    }

    /// <summary>
    /// Realiza o processo de login
    /// </summary>
    /// <returns></returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    public string Login([FromBody] LoginRecord credentials)
    {
        var user = _context.users.AsNoTracking().Where(x => x.email == credentials.email).FirstOrDefault();
        if(user is null){
            throw new ExpectedException("Usuário nao encontrado ou senha incorreta", System.Net.HttpStatusCode.Unauthorized);
        }

        if(!user.CheckPass(credentials.password)){
            throw new ExpectedException("Usuário nao encontrado ou senha incorreta",System.Net.HttpStatusCode.Unauthorized);
        }


        ITokenService token_service = new TokenServiceJWT();

        // Gera o Token
        var token = token_service.Generate(user, "Common User");

        //obtem o tempo de expiraçao configurado no token -> NÃO ESTÁ SENDO RESPEITADO E ESTÁ TRAZENDO SEMPRE 2H
        //var claims = (ClaimsPrincipal) token_service.GetTokenData(token);
        //var tokenExp = User.Claims.First(claim => claim.Type.Equals("exp")).Value;
        //var expiration = DateTime.UnixEpoch.AddSeconds(long.Parse(tokenExp));

        
        TimeSpan timeSpanUntilExpiration = DateTime.UtcNow.AddDays(30) - DateTime.UtcNow; 

        _redis.StringSetAsync(token, user.GetJsonSerialized(), timeSpanUntilExpiration);


        return token;
    }



    /// <summary>
    /// Realiza a renovação do token
    /// </summary>
    /// <returns></returns>
    // [HttpPost("renew")]
    // [AllowAnonymous]
    // [ProducesResponseType(typeof(string),StatusCodes.Status200OK)]
    // [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    // [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    // public string Renew([FromHeader] string Authorization){
    //     string token = Authorization.Split(" ")[1];
    //     ITokenService token_service = new TokenServiceJWT();
    //     try
    //     {
    //         token_service.Validate(token);
    //     }
    //     catch (System.Exception)
    //     {
    //         throw new ExpectedException("Validação de token falhou", System.Net.HttpStatusCode.Forbidden);
    //     }
        

    //     var user = _context.users.AsNoTracking().Where(x => x.email == User.Identity.Name).FirstOrDefault();

        
    //     var token_new = token_service.Renew(token);

    //     TimeSpan timeSpanUntilExpiration = DateTime.UtcNow.AddDays(30) - DateTime.UtcNow; 
        
    //     _redis.StringSetAsync(token_new, user.GetJsonSerialized(), timeSpanUntilExpiration);

    //     return token_new;
    // }
}
