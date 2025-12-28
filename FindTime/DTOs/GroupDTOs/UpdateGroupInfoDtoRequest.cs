namespace FindTime.DTOs.GroupDTOs;

public class UpdateGroupInfoDtoRequest
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
}