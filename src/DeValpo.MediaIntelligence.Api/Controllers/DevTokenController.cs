using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/dev/token")]
public sealed class DevTokenController(
    IDevJwtTokenService tokenService,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost]
    public IActionResult Create([FromBody] CreateDevTokenRequest? request)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(tokenService.CreateToken(request ?? new CreateDevTokenRequest(null, null, null)));
    }
}
