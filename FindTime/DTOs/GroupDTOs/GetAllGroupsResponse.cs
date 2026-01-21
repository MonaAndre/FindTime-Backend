namespace FindTime.DTOs.GroupDTOs;

public class GetAllGroupsResponse
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
    public string GroupColor { get; set; }
}