using BizSpace.Dtos;
using BizSpace.Models;
using BizSpace.Repositories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizSpace.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService categories) : BaseApiController
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetAll() =>
        Ok(ApiResponse<List<CategoryDto>>.Ok(await categories.GetAllAsync()));

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetById(int id) =>
        Ok(ApiResponse<CategoryDto>.Ok(await categories.GetByIdAsync(id)));

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Create(CreateCategoryRequest request)
    {
        var category = await categories.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, ApiResponse<CategoryDto>.Ok(category, "Category created."));
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Update(int id, UpdateCategoryRequest request) =>
        Ok(ApiResponse<CategoryDto>.Ok(await categories.UpdateAsync(id, request), "Category updated."));

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost("{id:int}/image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(9 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 8 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UploadImage(
        int id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null)
            throw new ArgumentException("Choose an image file to upload.");

        await using var content = file.OpenReadStream();
        var category = await categories.UploadImageAsync(
            id, content, file.Length, file.ContentType, cancellationToken);
        return Ok(ApiResponse<CategoryDto>.Ok(category, "Category image uploaded; previous Cloudinary image cleanup is queued."));
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await categories.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(null, "Category deleted."));
    }
}
