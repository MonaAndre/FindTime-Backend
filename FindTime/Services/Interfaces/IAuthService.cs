using FindTime.Common;
using FindTime.DTOs.AuthDTOs;

namespace FindTime.Services.Interfaces;

public interface IAuthService
{
    Task<ServiceResponse<AuthResponseDto>> RegisterAsync(RegisterDtoRequest dto);
    Task<ServiceResponse<AuthResponseDto>> LoginAsync(LoginDtoRequest dto);
    Task<ServiceResponse<bool>> LogoutAsync();
}