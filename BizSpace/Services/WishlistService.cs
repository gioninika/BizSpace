using BizSpace.Dtos;
using BizSpace.Exceptions;
using BizSpace.Models;
using BizSpace.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Repositories.Services;

public sealed class WishlistService(
    IGenericRepository<WishlistItem> wishlistItems,
    IGenericRepository<Product> products,
    IPricingService pricing) : IWishlistService
{
    public async Task<List<WishlistDto>> GetMineAsync(int userId)
    {
        var items = await wishlistItems.Query.AsNoTracking().Include(x => x.Product).ThenInclude(x => x!.Category)
            .Include(x => x.Product).ThenInclude(x => x!.Images)
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.ProductId)
            .ToListAsync();
        var productList = items.Where(x => x.Product is not null).Select(x => x.Product!).ToList();
        var discounts = await pricing.GetDiscountsAsync(productList);
        return items.Select(x => new WishlistDto
        {
            ProductId = x.ProductId,
            ProductName = x.Product!.Name,
            Price = ProductService.EffectivePrice(x.Product!, discounts[x.ProductId]),
            ImageUrl = x.Product.Images.OrderBy(image => image.SortOrder)
                .Select(image => image.SecureUrl).FirstOrDefault(),
            AddedAtUtc = x.CreatedAtUtc
        }).ToList();
    }

    public async Task AddAsync(int userId, int productId)
    {
        if (!await products.Query.AnyAsync(x => x.Id == productId))
            throw new KeyNotFoundException("Product was not found.");
        if (await wishlistItems.Query.AnyAsync(x => x.UserId == userId && x.ProductId == productId))
            return;
        await wishlistItems.AddAsync(new WishlistItem { UserId = userId, ProductId = productId });
        await wishlistItems.SaveAsync();
    }

    public async Task RemoveAsync(int userId, int productId)
    {
        var item = await wishlistItems.Query.FirstOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId)
            ?? throw new KeyNotFoundException("Wishlist item was not found.");
        wishlistItems.Delete(item);
        await wishlistItems.SaveAsync();
    }
}
