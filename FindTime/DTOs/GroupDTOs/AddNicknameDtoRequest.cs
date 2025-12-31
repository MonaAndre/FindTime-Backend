namespace FindTime.DTOs.GroupDTOs;

public class AddNicknameDtoRequest
{
    public string TargetUserId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public int GroupId { get; set; }
}