namespace FindTime.DTOs.AuthDTOs;

public class UserProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? Birthday { get; set; }
    public DateTime RegisterDay { get; set; }
    public string? ProfilePicLink { get; set; }
}