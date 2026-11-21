using System.Data;
using BizSpace.Dtos;
using BizSpace.Exceptions;
using BizSpace.Models;
using BizSpace.Repositories;
using BizSpace.Services.Images;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Repositories.Services;

public sealed class ProductService(
    IGenericRepository<Product> repository,
    IGenericRepository<Category> categories,
    IGenericRepository<Store> stores,
    IGenericRepository<ProductImage> productImageRecords,
    IGenericRepository<ProductImageDeletionJob> imageDeletionJobs,
    IGenericRepository<CartItem> cartItems,
    IGenericRepository<OrderItem> orderItems,
    IGenericRepository<WishlistItem> wishlistItems,
    IGenericRepository<ProductReview> reviewRecords,
    IGenericRepository<MarketingCampaignProduct> campaignProducts,
    INotificationService notifications,
    IPricingService pricing,
    IProductImageStorage imageStorage,
    IUnitOfWork unitOfWork,
    ILogger<ProductService> logger) : IProductService
{
    private const int MaxImagesPerProduct = 10;

    public async Task<PagedResult<ProductDto>> GetAllAsync(ProductQuery filter)
    {
        IQueryable<Product> query = repository.Query.AsNoTracking()
            .Include(x => x.Reviews)
            .Include(x => x.Images)
            .Include(x => x.Category);
        if (filter.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == filter.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Brand))
        {
            var brand = filter.Brand.Trim().ToUpperInvariant();
            query = query.Where(x => x.Brand.ToUpper() == brand);
        }
        if (filter.StoreId.HasValue)
            query = query.Where(x => x.StoreId == filter.StoreId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || x.Brand.Contains(search) || x.Description.Contains(search));
        }
        if (filter.MinPrice > filter.MaxPrice)
            throw new ArgumentException("Minimum price cannot be greater than maximum price.");

        int totalCount;
        List<Product> products;
        if (!filter.MinPrice.HasValue && !filter.MaxPrice.HasValue)
        {
            totalCount = await query.CountAsync();
            products = await query.OrderBy(x => x.Id)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();
        }
        else
        {
            var matches = await query.ToListAsync();
            var discounts = await pricing.GetDiscountsAsync(matches);
            matches = matches.Where(product =>
            {
                var effectivePrice = EffectivePrice(product, discounts[product.Id]);
                return (!filter.MinPrice.HasValue || effectivePrice >= filter.MinPrice.Value) &&
                    (!filter.MaxPrice.HasValue || effectivePrice <= filter.MaxPrice.Value);
            }).OrderBy(x => x.Id).ToList();
            totalCount = matches.Count;
            products = matches.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();
        }
        var productDtos = await ToDtosAsync(products);
        return new PagedResult<ProductDto>
        {
            Items = productDtos,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductDto> GetByIdAsync(int id)
    {
        var product = await repository.Query.Include(x => x.Reviews).Include(x => x.Images).Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Product was not found.");
        product.ViewCount++;
        await repository.SaveAsync();
        return await ToDtoAsync(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, int managerId)
    {
        var (store, category) = await ValidateStoreAndCategoryAsync(request.StoreId, request.CategoryId, managerId);
        var product = new Product();
        Apply(product, request);
        product.Store = store;
        product.Category = category;
        await repository.AddAsync(product);
        await repository.SaveAsync();
        await notifications.NotifyAdminsAsync(NotificationType.ProductCreated, "Product created",
            $"{product.Name} was added to the {store.Name} store.", store.Id, product.Id);
        if (product.Stock <= product.MinimumStockLevel && store.ManagerId is not null)
            await notifications.NotifyStoreManagerAsync(store.Id, NotificationType.LowStock, "Low stock",
                $"{product.Name} has stock {product.Stock}, at or below its minimum level of {product.MinimumStockLevel}.", product.Id);
        return await ToDtoAsync(product);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, int managerId)
    {
        var product = await repository.Query.Include(x => x.Store).Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Product was not found.");
        if (product.Store?.ManagerId != managerId)
            throw new ForbiddenException("You can update only products in your own stores.");
        var wasLowStock = product.Stock <= product.MinimumStockLevel;
        var (store, category) = await ValidateStoreAndCategoryAsync(request.StoreId, request.CategoryId, managerId);
        Apply(product, request);
        product.Store = store;
        product.Category = category;
        repository.Update(product);
        await repository.SaveAsync();
        if (!wasLowStock && product.Stock <= product.MinimumStockLevel && store.ManagerId is not null)
            await notifications.NotifyStoreManagerAsync(store.Id, NotificationType.LowStock, "Low stock",
                $"{product.Name} has stock {product.Stock}, at or below its minimum level of {product.MinimumStockLevel}.", product.Id);
        return await ToDtoAsync(product);
    }

    public async Task DeleteAsync(int id, int callerId, bool isAdmin)
    {
        await using var transaction = unitOfWork.BeginTransaction(IsolationLevel.Serializable);
        var product = await repository.Query.Include(x => x.Store).Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Product was not found.");
        if (!isAdmin && product.Store?.ManagerId != callerId)
            throw new ForbiddenException("You can delete only products in your own stores.");

        var productCartItems = await cartItems.Query.Where(x => x.ProductId == id).ToListAsync();
        var productOrderItems = await orderItems.Query.Where(x => x.ProductId == id).ToListAsync();
        var productWishlistItems = await wishlistItems.Query.Where(x => x.ProductId == id).ToListAsync();
        var productReviewRecords = await reviewRecords.Query.Where(x => x.ProductId == id).ToListAsync();
        var productCampaignLinks = await campaignProducts.Query.Where(x => x.ProductId == id).ToListAsync();

        foreach (var image in product.Images.Where(image => !string.IsNullOrWhiteSpace(image.PublicId)))
            await imageDeletionJobs.AddAsync(new ProductImageDeletionJob { PublicId = image.PublicId! });

        cartItems.DeleteRange(productCartItems);
        wishlistItems.DeleteRange(productWishlistItems);
        reviewRecords.DeleteRange(productReviewRecords);
        campaignProducts.DeleteRange(productCampaignLinks);
        foreach (var orderItem in productOrderItems)
            orderItem.ProductId = null;

        // Keep order snapshots while removing the references that would prevent product deletion.
        await unitOfWork.SaveChangesAsync();
        repository.Delete(product);
        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<ProductImageDto> UploadImageAsync(
        int id,
        Stream content,
        long contentLength,
        string contentType,
        int managerId,
        CancellationToken cancellationToken = default)
    {
        if (contentLength <= 0 || contentLength > ImageContentValidator.MaxImageBytes)
            throw new ArgumentException("An image must be between 1 byte and 8 MB.");

        var actualContentType = await ImageContentValidator.DetectContentTypeAsync(content, contentType, cancellationToken);
        var product = await GetOwnedProductForImagesAsync(id, managerId);
        if (product.Images.Count >= MaxImagesPerProduct)
            throw new BusinessRuleException("A product can have at most 10 images.");

        var uploaded = await imageStorage.UploadAsync(
            content, actualContentType, ImageEntityType.Product, product.Id, cancellationToken);
        var image = new ProductImage
        {
            ProductId = product.Id,
            Product = product,
            PublicId = uploaded.PublicId,
            SecureUrl = uploaded.SecureUrl,
            Format = uploaded.Format,
            Width = uploaded.Width,
            Height = uploaded.Height,
            SortOrder = product.Images.Count == 0 ? 0 : product.Images.Max(x => x.SortOrder) + 1
        };
        product.Images.Add(image);

        try
        {
            await repository.SaveAsync();
        }
        catch (Exception saveException)
        {
            try
            {
                await imageStorage.DeleteAsync(uploaded.PublicId, CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                logger.LogError(cleanupException,
                    "Could not remove Cloudinary asset {PublicId} after image record persistence failed.", uploaded.PublicId);
            }

            logger.LogError(saveException, "Could not save image record for product {ProductId}.", id);
            throw;
        }

        return ToImageDto(image);
    }

    public async Task<List<ProductImageDto>> ReorderImagesAsync(
        int id,
        ReorderProductImagesRequest request,
        int managerId)
    {
        var product = await GetOwnedProductForImagesAsync(id, managerId);
        var currentIds = product.Images.Select(x => x.Id).ToHashSet();
        if (request.ImageIds.Count != currentIds.Count ||
            request.ImageIds.Count != request.ImageIds.Distinct().Count() ||
            !request.ImageIds.ToHashSet().SetEquals(currentIds))
            throw new ArgumentException("ImageIds must contain each current product image exactly once.");

        var imagesById = product.Images.ToDictionary(x => x.Id);
        for (var index = 0; index < request.ImageIds.Count; index++)
            imagesById[request.ImageIds[index]].SortOrder = index;

        await repository.SaveAsync();
        return product.Images.OrderBy(x => x.SortOrder).Select(ToImageDto).ToList();
    }

    public async Task DeleteImageAsync(int id, int imageId, int managerId)
    {
        var product = await GetOwnedProductForImagesAsync(id, managerId);
        var image = product.Images.FirstOrDefault(x => x.Id == imageId)
            ?? throw new KeyNotFoundException("Product image was not found.");

        if (!string.IsNullOrWhiteSpace(image.PublicId))
            await imageDeletionJobs.AddAsync(new ProductImageDeletionJob { PublicId = image.PublicId });
        product.Images.Remove(image);
        productImageRecords.Delete(image);
        foreach (var remainingImage in product.Images.OrderBy(x => x.SortOrder).Select((item, index) => (item, index)))
            remainingImage.item.SortOrder = remainingImage.index;
        await repository.SaveAsync();
    }

    public async Task<ProductStatsDto> GetStatsAsync(int id, int callerId, bool isAdmin)
    {
        var product = await repository.Query.Include(x => x.Reviews).Include(x => x.Store)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Product was not found.");
        if (!isAdmin && product.Store?.ManagerId != callerId)
            throw new ForbiddenException("You can view statistics only for products in your own stores.");
        return new ProductStatsDto
        {
            ProductId = id,
            Views = product.ViewCount,
            Reviews = product.Reviews.Count,
            AverageRating = product.Reviews.Count == 0 ? 0 : product.Reviews.Average(x => x.Rating),
            Stock = product.Stock,
            IsLowStock = product.Stock <= product.MinimumStockLevel
        };
    }

    private async Task<(Store Store, Category Category)> ValidateStoreAndCategoryAsync(
        int storeId,
        int categoryId,
        int managerId)
    {
        var store = await stores.Query.FirstOrDefaultAsync(x => x.Id == storeId)
            ?? throw new KeyNotFoundException("Store was not found.");
        if (store.ManagerId != managerId)
            throw new ForbiddenException("Products must belong to one of your own stores.");

        var category = await categories.Query.FirstOrDefaultAsync(x => x.Id == categoryId)
            ?? throw new KeyNotFoundException("Category was not found.");
        return (store, category);
    }

    private async Task<Product> GetOwnedProductForImagesAsync(int id, int managerId)
    {
        var product = await repository.Query.Include(x => x.Store).Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Product was not found.");
        if (product.Store?.ManagerId != managerId)
            throw new ForbiddenException("You can manage images only for products in your own stores.");
        return product;
    }

    private static void Apply(Product product, ProductWriteRequest request)
    {
        product.Name = request.Name.Trim();
        product.Brand = request.Brand.Trim();
        product.Description = request.Description.Trim();
        product.Price = request.Price;
        product.DiscountPercent = request.DiscountPercent;
        product.StoreId = request.StoreId;
        product.CategoryId = request.CategoryId;
        product.Stock = request.Stock;
        product.MinimumStockLevel = request.MinimumStockLevel;
    }

    internal static ProductDto ToDto(Product product) => ToDto(product, product.DiscountPercent);

    internal static ProductDto ToDto(Product product, decimal effectiveDiscountPercent) => new()
    {
        Id = product.Id,
        StoreId = product.StoreId,
        Name = product.Name,
        Brand = product.Brand,
        Description = product.Description,
        Price = product.Price,
        DiscountPercent = effectiveDiscountPercent,
        EffectivePrice = EffectivePrice(product, effectiveDiscountPercent),
        CategoryId = product.CategoryId,
        Stock = product.Stock,
        MinimumStockLevel = product.MinimumStockLevel,
        Images = product.Images.OrderBy(x => x.SortOrder).Select(ToImageDto).ToList(),
        CreatedAtUtc = product.CreatedAtUtc,
        AverageRating = product.Reviews.Count == 0 ? 0 : product.Reviews.Average(x => x.Rating),
        ReviewCount = product.Reviews.Count
    };

    private static ProductImageDto ToImageDto(ProductImage image) => new()
    {
        Id = image.Id,
        SecureUrl = image.SecureUrl,
        Format = image.Format,
        Width = image.Width,
        Height = image.Height,
        SortOrder = image.SortOrder
    };

    internal static decimal EffectivePrice(Product product, decimal discountPercent) =>
        decimal.Round(product.Price * (1 - discountPercent / 100m), 2);

    private async Task<ProductDto> ToDtoAsync(Product product)
    {
        var discounts = await pricing.GetDiscountsAsync([product]);
        return ToDto(product, discounts[product.Id]);
    }

    private async Task<List<ProductDto>> ToDtosAsync(IReadOnlyCollection<Product> products)
    {
        var discounts = await pricing.GetDiscountsAsync(products);
        return products.Select(product => ToDto(product, discounts[product.Id])).ToList();
    }
}
