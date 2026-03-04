using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using api.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace api.Controllers;

/// <summary>
/// Controller de investimentos
/// </summary>
[Authorize]
[Route("[controller]")]
public class InvestmentController : Controller
{
    private readonly ILogger<InvestmentController> _logger;

    public InvestmentController(ILogger<InvestmentController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Lista os investimentos do usuário
    /// </summary>
    /// <returns></returns>
    [HttpGet("/investments/list")]
    public async Task<IActionResult> GetList()
    {
        return Ok(Result.Success());
    }


    /// <summary>
    /// Busca um investimento pelo id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("/investments/{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        return Ok(Result.Success());
    }
}
