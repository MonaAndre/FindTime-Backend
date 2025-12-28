using FindTime.Common;
using FindTime.DTOs.GroupDTOs;

namespace FindTime.Services.Interfaces;

public interface IGroupService
{
    Task<ServiceResponse<CreateGroupDtoResponse>> CreateGroupAsync(CreateGroupDtoRequest dto,
        string userId);

    Task<ServiceResponse<bool>> UpdateGroupInfoAsync(UpdateGroupInfoDtoRequest dto, string userId);
    Task<ServiceResponse<List<GetAllGroupsResponse>>> GetAllGroupsAsync(string userId);
    Task<ServiceResponse<GroupInfoDtoResponse>> GetGroupInfoAsync(string userId, int groupId);
}