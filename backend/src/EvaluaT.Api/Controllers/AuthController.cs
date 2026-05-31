using EvaluaT.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EvaluaT.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _auth.LoginAsync(request, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("register-student")]
    public async Task<ActionResult<AuthResponse>> RegisterStudent(
        RegisterStudentRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _auth.RegisterStudentAsync(request, cancellationToken));
    }
}
