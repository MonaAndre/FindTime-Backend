namespace FindTime.DTOs.GroupDTOs;

public class GroupInfoDtoResponse
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }  
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime JoinedAt { get; set; }
    public List<GroupMemberGroupDto> Members { get; set; } = new();
}

public class GroupMemberGroupDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfilePictureLink { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsAdmin { get; set; }
   
}