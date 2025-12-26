namespace FindTime.DTOs.GroupDTOs;

public class CreateGroupDtoRequest
{
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> MembersEmails { get; set; } = new List<string>();
}

