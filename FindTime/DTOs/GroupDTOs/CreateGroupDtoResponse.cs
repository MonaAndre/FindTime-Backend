namespace FindTime.DTOs.GroupDTOs;

public class CreateGroupDtoResponse
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public List<GroupMemberDto> Members { get; set; } = new List<GroupMemberDto>();
    public List<string> FailedEmails { get; set; } = new List<string>(); 
}