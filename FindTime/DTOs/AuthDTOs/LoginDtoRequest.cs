namespace FindTime.DTOs.AuthDTOs;

public class LoginDtoRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}