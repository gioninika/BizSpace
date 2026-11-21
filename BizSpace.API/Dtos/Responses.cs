using BizSpace.Models;

namespace BizSpace.Dtos;

public sealed class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class RegistrationResponse
{
    public string Email { get; set; } = string.Empty;
}

public sealed class UserDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class StoreDto
{
    public int Id { get; set; }
    public int? ManagerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class SavedAddressDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class MarketingCampaignDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsCurrentlyActive { get; set; }
    public int? StoreId { get; set; }
    public int? CategoryId { get; set; }
    public List<int> ProductIds { get; set; } = [];
}

public sealed class NotificationDto
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? StoreId { get; set; }
    public int? RelatedEntityId { get; set; }
    public int? SenderUserId { get; set; }
    public string? SenderName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
    public bool IsRead => ReadAtUtc.HasValue;
}

public sealed class SalesAnalyticsDto
{
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public int TotalOrders { get; set; }
    public int PaidOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal Revenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int UnitsSold { get; set; }
    public List<TopSellingProductDto> TopProducts { get; set; } = [];
}

public sealed class TopSellingProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class ProductDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal EffectivePrice { get; set; }
    public int CategoryId { get; set; }
    public int Stock { get; set; }
    public int MinimumStockLevel { get; set; }
    public List<ProductImageDto> Images { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public sealed class ProductImageDto
{
    public int Id { get; set; }
    public string SecureUrl { get; set; } = string.Empty;
    public string? Format { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary => SortOrder == 0;
}

public sealed class ProductStatsDto
{
    public int ProductId { get; set; }
    public long Views { get; set; }
    public int Reviews { get; set; }
    public double AverageRating { get; set; }
    public int Stock { get; set; }
    public bool IsLowStock { get; set; }
}

public sealed class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public List<CategoryDto> Children { get; set; } = [];
    public List<ProductDto> Products { get; set; } = [];
}

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed class CartDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public List<CartLineDto> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public int ItemCount { get; set; }
}

public sealed class CartLineDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public int AvailableStock { get; set; }
}

public sealed class OrderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public ShippingAddressRequest ShippingAddress { get; set; } = new();
    public string PaymentMethod { get; set; } = string.Empty;
    public string DeliveryMethod { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public List<OrderLineDto> Items { get; set; } = [];
}

public sealed class OrderLineDto
{
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class ProductReviewDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class WishlistDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime AddedAtUtc { get; set; }
}
