using Microsoft.AspNetCore.Mvc;
using server.Domain;

namespace server.Controllers;

[Route("[controller]")]
public class Banks : AuthenticatedController
{

    Context _context;
	ILogger _logger;

    public Banks(IHttpContextAccessor httpContextAccessor, Context context) : base(httpContextAccessor)
    {
		_context = context;
    }


    [ProducesResponseType(typeof(List<string>),StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
    [HttpGet("/Banks")]
    public List<string> Get() 
    {
        var banks = _context.investments.Where(x => x.owner.id == _user.id).Select(x => x.bank).Distinct().ToList();
        return banks;
    }
}
