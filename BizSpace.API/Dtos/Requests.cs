using System.ComponentModel.DataAnnotations;
using BizSpace.Models;

namespace BizSpace.Dtos;

public sealed class RegisterRequest
{
    [Required, StringLength(120, MinimumLength = 2)] public string DisplayName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8), StringLength(128)] public string Password { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class EmailOnlyRequest
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
}

public class VerifyEmailRequest : EmailOnlyRequest
{
    [Required, RegularExpression(@"^\d{6}$")] public string Code { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequest : VerifyEmailRequest
{
    [Required, MinLength(8), StringLength(128)] public string NewPassword { get; set; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required, MinLength(8), StringLength(128)] public string NewPassword { get; set; } = string.Empty;
}

public sealed class UpdateUserRequest
{
    [Required, StringLength(120, MinimumLength = 2)] public string DisplayName { get; set; } = string.Empty;
    [EmailAddress, StringLength(254)] public string? Email { get; set; }
}

public sealed class CreateStoreManagerRequest
{
    [Required, StringLength(120, MinimumLength = 2)] public string DisplayName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8), StringLength(128)] public string Password { get; set; } = string.Empty;
}

public sealed class CreateStoreRequest
{
    [Required, StringLength(160, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [StringLength(2000)] public string Description { get; set; } = string.Empty;
}

public sealed class SendManagerMessageRequest
{
    [Required, StringLength(2000, MinimumLength = 1)] public string Message { get; set; } = string.Empty;
}

public sealed class SavedAddressWriteRequest
{
    [Required, StringLength(80, MinimumLength = 1)] public string Label { get; set; } = string.Empty;
    [Required, StringLength(200)] public string StreetAddress { get; set; } = string.Empty;
    [Required, StringLength(100)] public string City { get; set; } = string.Empty;
    [Required, StringLength(20)] public string PostalCode { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Country { get; set; } = string.Empty;
}

public sealed class MarketingCampaignWriteRequest
{
    [Required, StringLength(160, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [StringLength(2000)] public string Description { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "90")] public decimal DiscountPercent { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public bool IsEnabled { get; set; } = true;
    [Range(1, int.MaxValue)] public int? StoreId { get; set; }
    [Range(1, int.MaxValue)] public int? CategoryId { get; set; }
    [Required, MaxLength(500)] public List<int> ProductIds { get; set; } = [];
}

public sealed class ProductQuery
{
    [StringLength(120)] public string? Search { get; set; }
    [StringLength(100)] public string? Brand { get; set; }
    public int? CategoryId { get; set; }
    [Range(1, int.MaxValue)] public int? StoreId { get; set; }
    [Range(typeof(decimal), "0", "999999999")] public decimal? MinPrice { get; set; }
    [Range(typeof(decimal), "0", "999999999")] public decimal? MaxPrice { get; set; }
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}

public class ProductWriteRequest
{
    [Required, StringLength(160, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [StringLength(100)] public string Brand { get; set; } = string.Empty;
    [Required, StringLength(4000)] public string Description { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "999999999")] public decimal Price { get; set; }
    [Range(typeof(decimal), "0", "90")] public decimal DiscountPercent { get; set; }
    [Range(1, int.MaxValue)] public int StoreId { get; set; }
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
    [Range(0, int.MaxValue)] public int Stock { get; set; }
    [Range(0, int.MaxValue)] public int MinimumStockLevel { get; set; }
}

public sealed class CreateProductRequest : ProductWriteRequest { }
public sealed class UpdateProductRequest : ProductWriteRequest { }

public sealed class ReorderProductImagesRequest
{
    [Required, MaxLength(10)] public List<int> ImageIds { get; set; } = [];
}

public class CategoryWriteRequest
{
    [Required, StringLength(100, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    [StringLength(2000)] public string Description { get; set; } = string.Empty;
    [Url, StringLength(1000)] public string? ImageUrl { get; set; }
}

public sealed class CreateCategoryRequest : CategoryWriteRequest { }
public sealed class UpdateCategoryRequest : CategoryWriteRequest { }

public sealed class AddCartItemRequest
{
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, 100)] public int Quantity { get; set; } = 1;
}

public sealed class UpdateCartQuantityRequest
{
    [Range(1, 100)] public int Quantity { get; set; }
}

public sealed class ShippingAddressRequest
{
    [Required, StringLength(200)] public string StreetAddress { get; set; } = string.Empty;
    [Required, StringLength(100)] public string City { get; set; } = string.Empty;
    [Required, StringLength(20)] public string PostalCode { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Country { get; set; } = string.Empty;
}

public sealed class CreateOrderRequest : IValidatableObject
{
    [Range(1, int.MaxValue)] public int? SavedAddressId { get; set; }
    public ShippingAddressRequest? ShippingAddress { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((SavedAddressId.HasValue && ShippingAddress is not null) ||
            (!SavedAddressId.HasValue && ShippingAddress is null))
            yield return new ValidationResult(
                "Provide either savedAddressId or shippingAddress, but not both.",
                [nameof(SavedAddressId), nameof(ShippingAddress)]);
    }
}

public sealed class CreateProductReviewRequest
{
    [Range(1, 5)] public int Rating { get; set; }
    [StringLength(2000)] public string Comment { get; set; } = string.Empty;
}
