using Microsoft.AspNetCore.Mvc;

namespace back.Controllers;

[ApiController]
public class LoginController : ControllerBase
{


    [HttpGet("/login")]
    public IActionResult Get()
    {
        return Ok();
    }
}
