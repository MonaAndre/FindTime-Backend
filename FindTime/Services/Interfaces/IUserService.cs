using FindTime.Common;
using FindTime.DTOs.UserDTOs;

namespace FindTime.Services.Interfaces;

public interface IUserService
{
    Task<ServiceResponse<UserDto>> GetUserAsync(string userId);
}