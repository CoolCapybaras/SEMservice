using System.Security.Claims;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Interfaces;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public sealed class SearchController : ControllerBase
{
    private readonly IGlobalSearchService _search;

    public SearchController(IGlobalSearchService search)
    {
        _search = search;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] GlobalSearchRequest request)
    {
        var userId = GetUserIdFromToken();
        var result = await _search.SearchAsync(request, userId);
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }

    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new Exception("Некорректный идентификатор пользователя в токене");

        return userId;
    }
}