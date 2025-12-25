namespace FindTime.DTOs.UserDTOs;

public class UserDto
{
    public string UserId { get; set; }=string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? Birthday { get; set; }
    public string? ProfilePicLink { get; set; }
    public string? PhoneNumber { get; set; }
    
}