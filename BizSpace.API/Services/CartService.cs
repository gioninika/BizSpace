using BizSpace.Dtos;
using BizSpace.Exceptions;
using BizSpace.Models;
using BizSpace.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Repositories.Services;

public sealed class CartService(
    IGenericRepository<Cart> carts,
    IGenericRepository<CartItem> cartItems,
    IGenericRepository<Product> products,
    IPricingService pricing) : ICartService
{
    public async Task<CartDto> GetMineAsync(int userId) => await ToDtoAsync(await LoadCartAsync(userId));

    public async Task<CartDto> AddItemAsync(int userId, AddCartItemRequest request)
    {
        var product = await products.Query.FirstOrDefaultAsync(x => x.Id == request.ProductId)
            ?? throw new KeyNotFoundException("Product was not found.");
        var cart = await LoadCartAsync(userId);
        var item = cart.Items.FirstOrDefault(x => x.ProductId == product.Id);
        var newQuantity = (item?.Quantity ?? 0) + request.Quantity;
        if (newQuantity > product.Stock)
            throw new BusinessRuleException("Requested quantity exceeds available stock.");
        if (item is null)
            cart.Items.Add(new CartItem { ProductId = product.Id, Quantity = request.Quantity });
        else
            item.Quantity = newQuantity;
        await carts.SaveAsync();
        return await ToDtoAsync(await LoadCartAsync(userId));
    }

    public async Task<CartDto> UpdateQuantityAsync(int userId, int itemId, int quantity)
    {
        var item = await cartItems.Query.Include(x => x.Product).Include(x => x.Cart)
            .FirstOrDefaultAsync(x => x.Id == itemId && x.Cart!.UserId == userId)
            ?? throw new KeyNotFoundException("Cart item was not found.");
        var product = item.Product ?? throw new KeyNotFoundException("Product was not found.");
        if (quantity > product.Stock)
            throw new BusinessRuleException("Requested quantity exceeds available stock.");
        item.Quantity = quantity;
        await cartItems.SaveAsync();
        return await ToDtoAsync(await LoadCartAsync(userId));
    }

    public async Task<CartDto> RemoveItemAsync(int userId, int itemId)
    {
        var item = await cartItems.Query.Include(x => x.Cart)
            .FirstOrDefaultAsync(x => x.Id == itemId && x.Cart!.UserId == userId)
            ?? throw new KeyNotFoundException("Cart item was not found.");
        cartItems.Delete(item);
        await cartItems.SaveAsync();
        return await ToDtoAsync(await LoadCartAsync(userId));
    }

    private async Task<Cart> LoadCartAsync(int userId)
    {
        var cart = await carts.Query.Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x!.Category)
            .Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x!.Images)
            .FirstOrDefaultAsync(x => x.UserId == userId);
        if (cart is not null) return cart;
        cart = new Cart { UserId = userId };
        await carts.AddAsync(cart);
        await carts.SaveAsync();
        return cart;
    }

    private async Task<CartDto> ToDtoAsync(Cart cart)
    {
        var products = cart.Items.Where(x => x.Product is not null).Select(x => x.Product!).ToList();
        var discounts = await pricing.GetDiscountsAsync(products);
        var items = cart.Items.Where(x => x.Product is not null).OrderBy(x => x.Id).Select(x => new CartLineDto
        {
            Id = x.Id,
            ProductId = x.ProductId,
            ProductName = x.Product!.Name,
            ImageUrl = x.Product.Images.OrderBy(image => image.SortOrder)
                .Select(image => image.SecureUrl).FirstOrDefault(),
            UnitPrice = ProductService.EffectivePrice(x.Product!, discounts[x.ProductId]),
            Quantity = x.Quantity,
            LineTotal = ProductService.EffectivePrice(x.Product!, discounts[x.ProductId]) * x.Quantity,
            AvailableStock = x.Product.Stock
        }).ToList();
        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = items,
            Subtotal = items.Sum(x => x.LineTotal),
            ItemCount = items.Sum(x => x.Quantity)
        };
    }
}
