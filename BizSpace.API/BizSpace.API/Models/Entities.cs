namespace BizSpace.Models;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string StoreManager = "StoreManager";
    public const string Customer = "Customer";
}

public enum NotificationType
{
    StoreCreated,
    CategoryCreated,
    ProductCreated,
    LowStock,
    NewOrder,
    AdminMessage,
    OrderStatusChanged,
    CustomerOrderCancelled
}

public static class AuthCodePurposes
{
    public const string ConfirmEmail = "ConfirmEmail";
    public const string ResetPassword = "ResetPassword";
}

public enum OrderStatus
{
    Pending,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}

public enum OrderStatusUpdate
{
    Paid = (int)OrderStatus.Paid,
    Shipped = (int)OrderStatus.Shipped,
    Delivered = (int)OrderStatus.Delivered
}

public enum PaymentMethod
{
    Card = 1,
    Cash = 2
}

public enum DeliveryMethod
{
    Pickup = 1,
    Courier = 2
}

public class UserAccount
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.Customer;
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedOutUntilUtc { get; set; }
    public bool EmailConfirmed { get; set; } = true;
    public string? EmailCodeHash { get; set; }
    public string? EmailCodePurpose { get; set; }
    public DateTime? EmailCodeExpiresAtUtc { get; set; }
    public DateTime? EmailCodeSentAtUtc { get; set; }
    public int EmailCodeFailedAttempts { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Cart? Cart { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    public ICollection<Store> ManagedStores { get; set; } = new List<Store>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Notification> SentNotifications { get; set; } = new List<Notification>();
    public ICollection<SavedAddress> Addresses { get; set; } = new List<SavedAddress>();
}

public class SavedAddress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public UserAccount? User { get; set; }
}

public class Store
{
    public int Id { get; set; }
    public int? ManagerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public UserAccount? Manager { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<MarketingCampaign> MarketingCampaigns { get; set; } = new List<MarketingCampaign>();
}

public class MarketingCampaign
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int? StoreId { get; set; }
    public int? CategoryId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Store? Store { get; set; }
    public Category? Category { get; set; }
    public ICollection<MarketingCampaignProduct> Products { get; set; } = new List<MarketingCampaignProduct>();
}

public class MarketingCampaignProduct
{
    public int CampaignId { get; set; }
    public int ProductId { get; set; }
    public MarketingCampaign? Campaign { get; set; }
    public Product? Product { get; set; }
}

public class Notification
{
    public int Id { get; set; }
    public int RecipientUserId { get; set; }
    public int? SenderUserId { get; set; }
    public int? StoreId { get; set; }
    public int? RelatedEntityId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
    public UserAccount? Recipient { get; set; }
    public UserAccount? Sender { get; set; }
    public Store? Store { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal DiscountPercent { get; set; }
    public int StoreId { get; set; }
    public int CategoryId { get; set; }
    public int Stock { get; set; }
    public int MinimumStockLevel { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int ViewCount { get; set; }
    public Store? Store { get; set; }
    public Category? Category { get; set; }
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    public ICollection<MarketingCampaignProduct> MarketingCampaigns { get; set; } = new List<MarketingCampaignProduct>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public decimal EffectivePrice => decimal.Round(Price * (1 - DiscountPercent / 100m), 2);
}

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? PublicId { get; set; }
    public string SecureUrl { get; set; } = string.Empty;
    public string? Format { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Product? Product { get; set; }
}

public class ProductImageDeletionJob
{
    public int Id { get; set; }
    public string PublicId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptAtUtc { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ImagePublicId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<MarketingCampaign> MarketingCampaigns { get; set; } = new List<MarketingCampaign>();
}

public class Cart
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public UserAccount? User { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public Cart? Cart { get; set; }
    public Product? Product { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public ShippingAddress ShippingAddress { get; set; } = new();
    public string PaymentMethod { get; set; } = string.Empty;
    public string DeliveryMethod { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public UserAccount? User { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class ShippingAddress
{
    public string StreetAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public Order? Order { get; set; }
    public Product? Product { get; set; }
}

public class WishlistItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public UserAccount? User { get; set; }
    public Product? Product { get; set; }
}

public class ProductReview
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public UserAccount? User { get; set; }
    public Product? Product { get; set; }
}
