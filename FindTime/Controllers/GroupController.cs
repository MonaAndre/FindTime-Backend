using System.Security.Claims;
using FindTime.DTOs.GroupDTOs;
using FindTime.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindTime.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GroupController(IGroupService groupService) : ControllerBase
{
    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ??
               throw new UnauthorizedAccessException("User not found");
    }

    [HttpPost("create-group")]
    public async Task<IActionResult> CreateGroup(CreateGroupDtoRequest dto)
    {
        var userId = GetUserId();
        var result = await groupService.CreateGroupAsync(dto, userId);

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("update-group-info")]
    public async Task<IActionResult> UpdateGroupInfo(UpdateGroupInfoDtoRequest dto)
    {
        var userId = GetUserId();
        var result = await groupService.UpdateGroupInfoAsync(dto, userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("get-all-groups")]
    public async Task<IActionResult> GetAllGroups()
    {
        var userId = GetUserId();
        var result = await groupService.GetAllGroupsAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("get-group/{groupId}")]
    public async Task<IActionResult> GetGroup(int groupId)
    {
        var userId = GetUserId();
        var result = await groupService.GetGroupInfoAsync(userId, groupId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("add-new-member")]
    public async Task<IActionResult> AddMemberToGroup(AddMemberToGroupDtoRequest dto)
    {
        var userId = GetUserId();
        var result = await groupService.AddMemberToGroupAsync(dto, userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("delete-member")]
    public async Task<IActionResult> DeleteMember(DeleteMemberDtoRequest dto)
    {
        var userId = GetUserId();
        var result = await groupService.DeleteMemberAsync(dto, userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("change-group-admin")]
    public async Task<IActionResult> ChangeGroupAdmin(ChangeAdminDtoRequest dto)
    {
        var userId = GetUserId();
        var result = await groupService.ChangeAdminAsync(dto, userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("leave-group")]
    public async Task<IActionResult> LeaveGroup(int groupId)
    {
        var userId = GetUserId();
        var result = await groupService.LeaveGroupAsync(groupId, userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("delete-group/{groupId}")]
    public async Task<IActionResult> DeleteGroup(int groupId)
    {
        var userId = GetUserId();
        var result = await groupService.DeleteGroupAsync(groupId, userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("add-nickname")]
    public async Task<IActionResult> AddNickname(AddNicknameDtoRequest dto)
    {
        var userId = GetUserId();
        var result = await groupService.AddNicknameToGroupMemberAsync(dto, userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("update-user-settings")]
    public async Task<IActionResult> UpdateUserGroupSettings(UpdateUserGroupSettingsDtoRequest dto)
    {
        var userId = GetUserId();
        var result = await groupService.UpdateUserGroupSettings(dto, userId);
        return StatusCode(result.StatusCode, result);
    }
}