namespace FindTime.DTOs.GroupDTOs;

public class AddMemberToGroupDtoRequest
{
    public int GroupId { get; set; }
    public string UserEmail { get; set; }
}