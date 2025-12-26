using FindTime.Common;
using FindTime.DTOs.GroupDTOs;

namespace FindTime.Services.Interfaces;

public interface IGroupService
{
    Task<ServiceResponse<CreateGroupDtoResponse>> CreateGroupAsync(CreateGroupDtoRequest dto,
        string userId);
}