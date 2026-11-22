using BizSpace.Dtos;
using BizSpace.Models;

namespace BizSpace.Repositories.Services;

public interface IAuthService
{
    Task<RegistrationResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request);
    Task ResendVerificationAsync(EmailOnlyRequest request);
    Task RequestPasswordResetAsync(EmailOnlyRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task SendVerificationEmailAsync(int userId);
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}

public interface IJwtService
{
    AuthResponse CreateToken(UserAccount user);
}

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetAllAsync(ProductQuery query);
    Task<ProductDto> GetByIdAsync(int id);
    Task<ProductDto> CreateAsync(CreateProductRequest request, int managerId);
    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, int managerId);
    Task DeleteAsync(int id, int callerId, bool isAdmin);
    Task<ProductImageDto> UploadImageAsync(int id, Stream content, long contentLength, string contentType, int managerId, CancellationToken cancellationToken = default);
    Task<List<ProductImageDto>> ReorderImagesAsync(int id, ReorderProductImagesRequest request, int managerId);
    Task DeleteImageAsync(int id, int imageId, int managerId);
    Task<ProductStatsDto> GetStatsAsync(int id, int callerId, bool isAdmin);
}

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto> GetByIdAsync(int id);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
    Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request);
    Task<CategoryDto> UploadImageAsync(int id, Stream content, long contentLength, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id);
}

public interface IStoreService
{
    Task<List<StoreDto>> GetAllAsync();
    Task<StoreDto> GetByIdAsync(int id);
    Task<List<StoreDto>> GetMineAsync(int managerId);
    Task<StoreDto> CreateAsync(int managerId, CreateStoreRequest request);
    Task DeleteAsync(int id, int callerId, bool isAdmin);
}

public interface INotificationService
{
    Task<List<NotificationDto>> GetMineAsync(int userId, bool unreadOnly);
    Task<NotificationDto> MarkReadAsync(int userId, int notificationId);
    Task<int> MarkAllReadAsync(int userId);
    Task NotifyAdminsAsync(NotificationType type, string title, string message, int? storeId, int? relatedEntityId = null);
    Task NotifyStoreManagerAsync(int storeId, NotificationType type, string title, string message, int? relatedEntityId = null);
    Task NotifyUserAsync(int userId, NotificationType type, string title, string message, int? relatedEntityId = null);
    Task<NotificationDto> SendAdminMessageAsync(int storeId, int adminId, string message);
}

public interface ISalesAnalyticsService
{
    Task<SalesAnalyticsDto> GetSalesAsync(DateTime? fromUtc, DateTime? toUtc, int? managerId = null);
}

public interface IAddressBookService
{
    Task<List<SavedAddressDto>> GetMineAsync(int userId);
    Task<SavedAddressDto> CreateAsync(int userId, SavedAddressWriteRequest request);
    Task<SavedAddressDto> UpdateAsync(int userId, int addressId, SavedAddressWriteRequest request);
    Task DeleteAsync(int userId, int addressId);
}

public interface IMarketingCampaignService
{
    Task<List<MarketingCampaignDto>> GetAllAsync(int managerId);
    Task<MarketingCampaignDto> CreateAsync(MarketingCampaignWriteRequest request, int managerId);
    Task<MarketingCampaignDto> UpdateAsync(int id, MarketingCampaignWriteRequest request, int managerId);
    Task DeleteAsync(int id, int managerId);
}

public interface IPricingService
{
    Task<Dictionary<int, decimal>> GetDiscountsAsync(IReadOnlyCollection<Product> products);
}

public interface ICartService
{
    Task<CartDto> GetMineAsync(int userId);
    Task<CartDto> AddItemAsync(int userId, AddCartItemRequest request);
    Task<CartDto> UpdateQuantityAsync(int userId, int itemId, int quantity);
    Task<CartDto> RemoveItemAsync(int userId, int itemId);
}

public interface IOrderService
{
    Task<List<OrderDto>> GetAllAsync(int userId, bool isAdmin);
    Task<OrderDto> GetByIdAsync(int id, int userId, bool isAdmin);
    Task<List<OrderDto>> GetForStoreManagerAsync(int managerId);
    Task<OrderDto> GetForStoreManagerAsync(int id, int managerId);
    Task<OrderDto> CreateFromCartAsync(int userId, CreateOrderRequest request, PaymentMethod paymentMethod, DeliveryMethod deliveryMethod);
    Task<OrderDto> UpdateStatusAsync(int id, OrderStatus nextStatus, int? managerId = null);
    Task<OrderDto> CancelAsync(int id, int userId, bool isAdmin, int? managerId = null);
    Task DeleteAsync(int id);
    Task<List<OrderDto>> GetForUserAsync(int targetUserId, int callerId, bool isAdmin);
}

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto> CreateStoreManagerAsync(CreateStoreManagerRequest request);
    Task<UserDto> GetByIdAsync(int id, int callerId, bool isAdmin);
    Task<UserDto> UpdateAsync(int id, int callerId, bool isAdmin, UpdateUserRequest request);
}

public interface IWishlistService
{
    Task<List<WishlistDto>> GetMineAsync(int userId);
    Task AddAsync(int userId, int productId);
    Task RemoveAsync(int userId, int productId);
}

public interface IProductReviewService
{
    Task<List<ProductReviewDto>> GetForProductAsync(int productId);
    Task<ProductReviewDto> CreateAsync(int userId, int productId, CreateProductReviewRequest request);
}
