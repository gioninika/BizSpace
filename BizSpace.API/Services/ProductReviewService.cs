using BizSpace.Dtos;
using BizSpace.Exceptions;
using BizSpace.Models;
using BizSpace.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Repositories.Services;

public sealed class ProductReviewService(
    IGenericRepository<Product> products,
    IGenericRepository<ProductReview> reviews,
    IGenericRepository<UserAccount> users) : IProductReviewService
{
    public async Task<List<ProductReviewDto>> GetForProductAsync(int productId)
    {
        if (!await products.Query.AnyAsync(x => x.Id == productId))
            throw new KeyNotFoundException("Product was not found.");
        return await reviews.Query.AsNoTracking().Where(x => x.ProductId == productId)
            .OrderBy(x => x.Id)
            .Select(x => new ProductReviewDto
            {
                Id = x.Id,
                UserId = x.UserId,
                DisplayName = x.User!.DisplayName,
                ProductId = x.ProductId,
                Rating = x.Rating,
                Comment = x.Comment,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToListAsync();
    }

    public async Task<ProductReviewDto> CreateAsync(int userId, int productId, CreateProductReviewRequest request)
    {
        var product = await products.Query.FirstOrDefaultAsync(x => x.Id == productId)
            ?? throw new KeyNotFoundException("Product was not found.");
        if (await reviews.Query.AnyAsync(x => x.UserId == userId && x.ProductId == productId))
            throw new BusinessRuleException("You have already reviewed this product.");
        var user = await users.Query.FirstAsync(x => x.Id == userId);
        var review = new ProductReview
        {
            UserId = userId,
            ProductId = product.Id,
            Rating = request.Rating,
            Comment = request.Comment.Trim()
        };
        await reviews.AddAsync(review);
        await reviews.SaveAsync();
        return new ProductReviewDto
        {
            Id = review.Id,
            UserId = userId,
            DisplayName = user.DisplayName,
            ProductId = productId,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAtUtc = review.CreatedAtUtc
        };
    }
}
