using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using api.repositories;
using api.services;
using back.domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace back.Controllers;

/// <summary>
/// Controller de login
/// </summary>
[ApiController]
public class LoginController : ControllerBase
{
    private readonly UserRepository _userRepository;
    private readonly IWebHostEnvironment _env;

    public LoginController(UserRepository userRepository, IWebHostEnvironment env)
    {
        _userRepository = userRepository;
        _env = env;
    }

    [HttpPost("/login")]
    public async Task<Result> Post(LoginRequest request)
    {
        var user = _userRepository.GetByEmail(request.Email);
        if (user is null)
            return Result.Failure("Usuário ou senha incorretos");
        
        if (!Encrypt.VerifyPassword(request.Password, user.Salt, user.Password))
            return Result.Failure("Usuário ou senha incorretos");

        // Gerar JWT e definir cookie de sessão
        string token = JwtService.GenerateToken(user);
        Response.Cookies.Append("session", token, BuildCookieOptions());

        return Result.Success();
    }

    /// <summary>
    /// Retorna os dados do usuário autenticado a partir do token JWT no cookie.
    /// </summary>
    [Authorize]
    [HttpGet("/login/me")]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var name = User.FindFirstValue("name");

        return Ok(new { id = userId, email, name });
    }

    /// <summary>
    /// Encerra a sessão do usuário removendo o cookie.
    /// </summary>
    [HttpPost("/login/logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("session", BuildCookieOptions());
        return Ok(Result.Success());
    }

    [HttpPost("/login/create")]
    public async Task<IActionResult> CreateAccount(CreateAccountRequest request)
    {
        string salt = Guid.NewGuid().ToString();
        User newUser = new()
        {
            Id = SnowflakeGuid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Password = Encrypt.HashPassword(request.Password, salt),
            Salt = salt
        };

        await _userRepository.Create(newUser);
        return Ok(newUser);
    }

    private CookieOptions BuildCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(), // false em dev (HTTP), true em produção (HTTPS)
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(JwtService.ExpirationDays),
            Path = "/"
        };
    }

    /// <summary>
    /// Requisição para login
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    public class LoginRequest
    {
        /// <summary>
        /// Email do usuário
        /// </summary>
        [Required] [EmailAddress] 
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Senha do usuário
        /// </summary>
        [Required] 
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Requisição para criar uma conta
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="name"></param>
    public class CreateAccountRequest
    {
        /// <summary>
        /// Email do usuário
        /// </summary>
        [Required] [EmailAddress] 
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Senha do usuário
        /// </summary>
        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Nome do usuário
        /// </summary>
        [Required] 
        [MinLength(3)]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}

