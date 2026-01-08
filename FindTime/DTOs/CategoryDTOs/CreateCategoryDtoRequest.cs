namespace FindTime.DTOs.CategoryDto;

public class CreateCategoryDtoRequest
{
    public int GroupId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategoryColor { get; set; }
}