namespace FindTime.DTOs.GroupDTOs;

public class ChangeAdminDtoRequest
{
    public int GroupId { get; set; }
    public string NewAdminUserId { get; set; }
}