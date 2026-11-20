using BizSpace.Dtos;
using BizSpace.Models;
using BizSpace.Repositories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizSpace.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductService products, IProductReviewService reviews) : BaseApiController
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetAll([FromQuery] ProductQuery query) =>
        Ok(ApiResponse<PagedResult<ProductDto>>.Ok(await products.GetAllAsync(query)));

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetById(int id) =>
        Ok(ApiResponse<ProductDto>.Ok(await products.GetByIdAsync(id)));

    [Authorize(Roles = UserRoles.StoreManager)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Create(CreateProductRequest request)
    {
        var product = await products.CreateAsync(request, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ApiResponse<ProductDto>.Ok(product, "Product created."));
    }

    [Authorize(Roles = UserRoles.StoreManager)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Update(int id, UpdateProductRequest request) =>
        Ok(ApiResponse<ProductDto>.Ok(await products.UpdateAsync(id, request, CurrentUserId), "Product updated."));

    [Authorize(Roles = UserRoles.StoreManager)]
    [HttpPost("{id:int}/images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(9 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 8 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ProductImageDto>>> UploadImage(
        int id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null)
            throw new ArgumentException("Choose an image file to upload.");

        await using var content = file.OpenReadStream();
        var image = await products.UploadImageAsync(
            id, content, file.Length, file.ContentType, CurrentUserId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<ProductImageDto>.Ok(image, "Product image uploaded."));
    }

    [Authorize(Roles = UserRoles.StoreManager)]
    [HttpPut("{id:int}/images/order")]
    public async Task<ActionResult<ApiResponse<List<ProductImageDto>>>> ReorderImages(
        int id,
        ReorderProductImagesRequest request) =>
        Ok(ApiResponse<List<ProductImageDto>>.Ok(
            await products.ReorderImagesAsync(id, request, CurrentUserId), "Product images reordered."));

    [Authorize(Roles = UserRoles.StoreManager)]
    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteImage(int id, int imageId)
    {
        await products.DeleteImageAsync(id, imageId, CurrentUserId);
        return Ok(ApiResponse<object>.Ok(new { }, "Product image removed. Cloudinary cleanup is queued."));
    }

    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.StoreManager)]
    [HttpGet("{id:int}/stats")]
    public async Task<ActionResult<ApiResponse<ProductStatsDto>>> GetStats(int id) =>
        Ok(ApiResponse<ProductStatsDto>.Ok(await products.GetStatsAsync(id, CurrentUserId, IsAdmin())));

    [AllowAnonymous]
    [HttpGet("{productId:int}/reviews")]
    public async Task<ActionResult<ApiResponse<List<ProductReviewDto>>>> GetReviews(int productId) =>
        Ok(ApiResponse<List<ProductReviewDto>>.Ok(await reviews.GetForProductAsync(productId)));

    [Authorize]
    [HttpPost("{productId:int}/reviews")]
    public async Task<ActionResult<ApiResponse<ProductReviewDto>>> CreateReview(int productId, CreateProductReviewRequest request)
    {
        var review = await reviews.CreateAsync(CurrentUserId, productId, request);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ProductReviewDto>.Ok(review, "Review created."));
    }
}
