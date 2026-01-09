namespace FindTime.DTOs.CategoryDTOs
{
    public class AddCategoryToEventDtoRequest
    {
        public int GroupId { get; set; }
        public int EventId { get; set; }
        public int CategoryId { get; set; } 
    }
}
