using FindTime.Common;
using FindTime.DTOs.CategoryDto;
using FindTime.DTOs.CategoryDTOs;

namespace FindTime.Services.Interfaces;

public interface ICategoryService
{
    Task<ServiceResponse<bool>> CreateCategoryAsync(CreateCategoryDtoRequest dto, string userId);
    Task<ServiceResponse<bool>> AddOrUpdateCategoryToEventAsync(AddCategoryToEventDtoRequest dto, string userId);
    Task<ServiceResponse<List<CategoryListDtoResponse>>> GetAllCategoriesAsync(int groupId, string userId);
    Task<ServiceResponse<bool>> UpdateCategoryAsync(UpdateCategoryRequestDto dto, string userId);
    Task<ServiceResponse<bool>> DeleteCategoryAsync(DeleteCategoryDtoRequest dto, string userId);
}