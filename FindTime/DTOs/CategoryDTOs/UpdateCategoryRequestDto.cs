namespace FindTime.DTOs.CategoryDTOs
{
    public class UpdateCategoryRequestDto
    {
        public int CategoryId { get; set; }
        public int GroupId  { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryColor { get; set; }
    }
}
