using Microsoft.AspNetCore.Mvc;

namespace Api.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TokenService _tokenService;

    public AuthController(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request.Username == "admin" && request.Password == "123")
        {
            var token = _tokenService.GenerateToken(request.Username);
            return Ok(new { token });
        }

        return Unauthorized("Invalid username or password");
    }
}

public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}
