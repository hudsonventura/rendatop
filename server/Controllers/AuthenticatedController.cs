using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Domain;
using server.Utils;

namespace server.Controllers;

[Authorize]
public abstract class AuthenticatedController : ControllerBase
{
    protected User _user;
    protected bool IsAdmin => _user.user_type == UserType.Admin;
    

    public AuthenticatedController(IHttpContextAccessor httpContextAccessor)
    {
        httpContextAccessor.HttpContext.Items.TryGetValue("User", out object user);
        if(user is null){
            throw new ExpectedException("Token de autenticação ausente ou inválido..", System.Net.HttpStatusCode.Unauthorized);
        }
        _user = (User)user; 
    }

    protected void EnsureAdmin()
    {
        if (!IsAdmin)
            throw new ExpectedException("Acesso permitido apenas para administradores.", System.Net.HttpStatusCode.Forbidden);
    }


}
