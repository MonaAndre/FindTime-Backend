using System.Security.Claims;
using FindTime.DTOs.CategoryDto;
using FindTime.DTOs.CategoryDTOs;
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
    [HttpPost("add-category")]
    public async Task<IActionResult> AddCategory(AddCategoryToEventDtoRequest dto)
    {
        var result = await categoryService.AddOrUpdateCategoryToEventAsync(dto, GetUserId());
        return StatusCode(result.StatusCode, result);
    }
    [HttpGet("get-categories/{groupId}")]
    public async Task<IActionResult> GetCategories(int groupId)
    {
        var result = await categoryService.GetAllCategoriesAsync(groupId, GetUserId());
        return StatusCode(result.StatusCode, result);
    }
    [HttpPost("update-category")]
    public async Task<IActionResult> UpdateCategory(UpdateCategoryRequestDto dto)
    {
        var result = await categoryService.UpdateCategoryAsync(dto, GetUserId());
        return StatusCode(result.StatusCode, result);
    }
}