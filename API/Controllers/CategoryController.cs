using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM.Domain.Interfaces;

namespace SEM.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoryController : ControllerBase
{
    private readonly IEventService _eventService;

    public CategoryController(IEventService eventService)
    {
        _eventService = eventService;
    }

    /// <summary>
    /// Получить все возможные категории
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllCategories()
    {
        var result = await _eventService.GetAllCategoriesAsync();
        return result.Success ? Ok(new { result = result.Data }) : BadRequest(new { error = result.Error });
    }
}