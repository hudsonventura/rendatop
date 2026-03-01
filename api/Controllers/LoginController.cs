using api.repositories;
using Microsoft.AspNetCore.Mvc;

namespace back.Controllers;

/// <summary>
/// Controller de login
/// </summary>
[ApiController]
public class LoginController : ControllerBase
{
    private readonly UserRepository _userRepository;

    public LoginController(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet("/login")]
    public IActionResult Get()
    {
        return Ok();
    }
}
