using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Models;
using SEM.Domain.Interfaces;

namespace SEM.API.Controllers;

[Route("api/auth")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserManager _userManager;

    public UserController(IUserManager userManager)
    {
        _userManager = userManager;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var token = await _userManager.RegisterAsync(request.Email, request.Password);
        if (token == "User already exists")
            return BadRequest("User already exists");

        return Ok(new { Token = token });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var token = await _userManager.LoginAsync(request.Email, request.Password);
        if (token == null)
            return Unauthorized("Invalid email or password");

        return Ok(new { Token = token });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var result = await _userManager.LogoutAsync();
        return result ? Ok("Logged out successfully") : BadRequest("Logout failed");
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var success = await _userManager.RequestPasswordResetAsync(request.Email);
        if (!success)
            return BadRequest("Email not found");

        return Ok("Password reset link sent");
    }

    /*[HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var success = await _userManager.ResetPasswordAsync(request.Token, request.NewPassword);
        if (!success)
            return BadRequest("Invalid or expired token");

        return Ok("Password successfully reset");
    }*/
}