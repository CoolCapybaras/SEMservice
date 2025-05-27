using System.Threading.Tasks;
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

    [HttpGet]
    public async Task<IActionResult> GetAllCategories()
    {
        var categories = await _eventService.GetAllCategoriesAsync();
        return Ok( new { result = categories});
    }
}