using System.Security.Claims;
using FindTime.DTOs.CategoryDto;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindTime.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoryController(ICategoryService categoryService) : ControllerBase
{
    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ??
               throw new UnauthorizedAccessException("User not found");
    }

    [HttpPost("create-category")]
    public async Task<IActionResult> CreateCategory(CreateCategoryDtoRequest dto)
    {
        var result = await categoryService.CreateCategoryAsync(dto, GetUserId());
        return StatusCode(result.StatusCode, result);
    }
}