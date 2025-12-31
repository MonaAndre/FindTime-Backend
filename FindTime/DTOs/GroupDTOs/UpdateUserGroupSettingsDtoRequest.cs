namespace FindTime.DTOs.GroupDTOs;

public class UpdateUserGroupSettingsDtoRequest
{
    public int GroupId { get; set; }
    public string GroupColor { get; set; } = string.Empty;
    
}