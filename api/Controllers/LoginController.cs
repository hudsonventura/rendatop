using System.ComponentModel.DataAnnotations;
using api.repositories;
using api.services;
using back.domain;
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


    [HttpPost("/login")]
    public IActionResult Post(LoginRequest request)
    {
        return Ok();
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
