using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Domain;
using StackExchange.Redis;

namespace server.Controllers;

/// <summary>
/// Home controller
/// </summary>
[ApiController]
public class HomeController​ : AuthenticatedController
{
    private readonly ILogger<HomeController​> _logger;
    private readonly Context _context;
    private StackExchange.Redis.IDatabase _redis;
    


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger"></param>
    public HomeController(ILogger<HomeController> logger, 
                            IHttpContextAccessor httpContextAccessor, 
                            Context context, 
                            IConnectionMultiplexer muxer_redis) 
        : base(httpContextAccessor)
    {
        _logger = logger;
        _context = context;
        _redis = muxer_redis.GetDatabase();
    }



    /// <summary>
    /// Permite testar a autenticação no backend. Se não autenticado, não passar o toke ou este estiver expirado, então retorna erro 401
    /// </summary>
    /// <returns>Aqui é a descrição do que esse troço retorna</returns>
    [ProducesResponseType(typeof(List<string>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpGet("authenticated")]
    [Authorize]
    public void Authenticated(){
        return;   
    } 



    /// <summary>
    /// Permite acesso somenteo do admin
    /// </summary>
    /// <returns>Retona um objeto qualquer</returns>
    [ProducesResponseType(typeof(string),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpGet("authenticated_admin")]
    [Authorize(Policy = "Somente_Admin")]
    public string Authenticated_Admin(){

        return _user.email;
        //return Ok(claims.Where(x => x.Type == "Sou admin").FirstOrDefault().Value);
    } 
}
