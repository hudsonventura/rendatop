using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Domain;
using server.Utils;

namespace server.Controllers;

[Authorize]
public abstract class AuthenticatedController : ControllerBase
{
    protected User _user;
    

    public AuthenticatedController(IHttpContextAccessor httpContextAccessor)
    {
        httpContextAccessor.HttpContext.Items.TryGetValue("User", out object user);
        if(user is null){
            throw new ExpectedException("Token de autenticação ausente ou inválido..", System.Net.HttpStatusCode.Unauthorized);
        }
        _user = (User)user; 
    }


}
