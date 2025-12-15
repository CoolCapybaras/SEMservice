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

    /// <summary>
    /// Регистрация пользователя
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _userManager.RegisterAsync(request.Email, request.Password);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { result.Data });
    }

    /// <summary>
    /// Авторизация пользователя
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _userManager.LoginAsync(request.Email, request.Password);
        if (!result.Success)
            return Unauthorized(new { error = result.Error });

        return Ok(new { result.Data });
    }

    /// <summary>
    /// Обновление Jwt токена через refresh
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _userManager.RefreshAsync(request.RefreshToken);
        if (!result.Success)
            return Unauthorized(new { error = result.Error });

        return Ok(new { result.Data });
    }
    
    /// <summary>
    /// Выход пользователя
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var result = await _userManager.LogoutAsync();
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok("Logged out successfully");
    }

    /// <summary>
    /// Восстановление пароля
    /// </summary>
    [HttpPost("recover-password")]
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