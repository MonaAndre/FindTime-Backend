using System.Security.Claims;
using FindTime.DTOs.GroupDTOs;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindTime.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GroupController : ControllerBase
{
    private readonly IGroupService _groupService;

    public GroupController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ??
               throw new UnauthorizedAccessException("User not found");
    }

    [HttpPost("create-group")]
    public async Task<IActionResult> CreateGroup(CreateGroupDtoRequest dto)
    {
        var userId = GetUserId();
        var result = await _groupService.CreateGroupAsync(dto, userId);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("update-group-info")]
    public async Task<IActionResult> UpdateGroupInfo(UpdateGroupInfoDtoRequest dto)
    {
        var userId = GetUserId();
        var result = await _groupService.UpdateGroupInfoAsync(dto, userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("get-all-groups")]
    public async Task<IActionResult> GetAllGroups()
    {
        var userId = GetUserId();
        var result = await _groupService.GetAllGroupsAsync(userId);
        return StatusCode(result.StatusCode, result);
    }
}