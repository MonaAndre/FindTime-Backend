using FindTime.Common;
using FindTime.DTOs.CategoryDto;

namespace FindTime.Services.Interfaces;

public interface ICategoryService
{ Task<ServiceResponse<bool>> CreateCategoryAsync(CreateCategoryDtoRequest dto, string userId);
}