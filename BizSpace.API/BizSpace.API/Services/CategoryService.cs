using BizSpace.Dtos;
using BizSpace.Exceptions;
using BizSpace.Models;
using BizSpace.Repositories;
using BizSpace.Services.Images;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Repositories.Services;

public sealed class CategoryService(
    IGenericRepository<Category> repository,
    IGenericRepository<ProductImageDeletionJob> imageDeletionJobs,
    IPricingService pricing,
    IProductImageStorage imageStorage,
    ILogger<CategoryService> logger) : ICategoryService
{
    public async Task<List<CategoryDto>> GetAllAsync()
    {
        IQueryable<Category> query = CategoriesWithProducts();
        var categories = await query.OrderBy(x => x.Id).ToListAsync();
        var rootIds = categories.Where(x => x.ParentId is null).Select(x => x.Id);
        return await BuildTreeAsync(categories, rootIds);
    }

    public async Task<CategoryDto> GetByIdAsync(int id)
    {
        var category = await CategoriesWithProducts().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Category was not found.");

        var categories = new List<Category> { category };
        var parentIds = new List<int> { id };
        while (parentIds.Count > 0)
        {
            var children = await CategoriesWithProducts()
                .Where(x => x.ParentId.HasValue && parentIds.Contains(x.ParentId.Value))
                .OrderBy(x => x.Id)
                .ToListAsync();
            if (children.Count == 0) break;

            categories.AddRange(children);
            parentIds = children.Select(x => x.Id).ToList();
        }

        return (await BuildTreeAsync(categories, [id])).Single();
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
    {
        await ValidateParentAndUniqueNameAsync(request.Name, request.ParentId, null);
        var category = new Category();
        Apply(category, request);
        category.ImagePublicId = imageStorage.TryGetPublicIdFromSecureUrl(category.ImageUrl);
        await repository.AddAsync(category);
        await repository.SaveAsync();
        return await GetByIdAsync(category.Id);
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request)
    {
        var category = await repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Category was not found.");
        var previousImageUrl = category.ImageUrl;
        var previousImagePublicId = category.ImagePublicId ??
            imageStorage.TryGetPublicIdFromSecureUrl(category.ImageUrl);

        if (request.ParentId == id || request.ParentId.HasValue &&
            await IsDescendantAsync(id, request.ParentId.Value))
            throw new BusinessRuleException("A category cannot be its own parent or a descendant of itself.");

        await ValidateParentAndUniqueNameAsync(request.Name, request.ParentId, id);
        Apply(category, request);
        if (!string.Equals(previousImageUrl, category.ImageUrl, StringComparison.Ordinal))
        {
            var nextImagePublicId = imageStorage.TryGetPublicIdFromSecureUrl(category.ImageUrl);
            category.ImagePublicId = nextImagePublicId;
            if (!string.Equals(previousImagePublicId, nextImagePublicId, StringComparison.Ordinal))
                await QueueCloudinaryDeletionAsync(previousImagePublicId);
        }
        repository.Update(category);
        await repository.SaveAsync();
        return await GetByIdAsync(category.Id);
    }

    public async Task<CategoryDto> UploadImageAsync(
        int id,
        Stream content,
        long contentLength,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (contentLength <= 0 || contentLength > ImageContentValidator.MaxImageBytes)
            throw new ArgumentException("An image must be between 1 byte and 8 MB.");

        var actualContentType = await ImageContentValidator.DetectContentTypeAsync(
            content, contentType, cancellationToken);
        var category = await repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Category was not found.");

        var previousImagePublicId = category.ImagePublicId ??
            imageStorage.TryGetPublicIdFromSecureUrl(category.ImageUrl);
        var uploaded = await imageStorage.UploadAsync(
            content, actualContentType, ImageEntityType.Category, category.Id, cancellationToken);

        category.ImageUrl = uploaded.SecureUrl;
        category.ImagePublicId = uploaded.PublicId;
        if (!string.IsNullOrWhiteSpace(previousImagePublicId) &&
            !string.Equals(previousImagePublicId, uploaded.PublicId, StringComparison.Ordinal))
            await imageDeletionJobs.AddAsync(new ProductImageDeletionJob { PublicId = previousImagePublicId });

        repository.Update(category);
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
                    "Could not remove Cloudinary asset {PublicId} after category image persistence failed.", uploaded.PublicId);
            }

            logger.LogError(saveException, "Could not save image record for category {CategoryId}.", id);
            throw;
        }

        return await GetByIdAsync(category.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var category = await repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Category was not found.");

        if (await repository.Query.AnyAsync(x => x.ParentId == id))
            throw new BusinessRuleException("A category cannot be deleted while it has child categories.");
        if (await repository.Query.AnyAsync(x => x.Id == id && x.Products.Any()))
            throw new BusinessRuleException("A category cannot be deleted while products reference it.");
        if (await repository.Query.AnyAsync(x => x.Id == id && x.MarketingCampaigns.Any()))
            throw new BusinessRuleException("A category cannot be deleted while campaigns reference it.");

        var categoryImagePublicId = category.ImagePublicId ??
            imageStorage.TryGetPublicIdFromSecureUrl(category.ImageUrl);
        await QueueCloudinaryDeletionAsync(categoryImagePublicId);
        repository.Delete(category);
        await repository.SaveAsync();
    }

    private async Task ValidateParentAndUniqueNameAsync(string name, int? parentId, int? currentId)
    {
        if (parentId.HasValue)
        {
            if (!await repository.Query.AnyAsync(x => x.Id == parentId.Value))
                throw new KeyNotFoundException("Parent category was not found.");
        }

        var normalized = name.Trim().ToUpperInvariant();
        if (await repository.Query.AnyAsync(x => (!currentId.HasValue || x.Id != currentId.Value) &&
            x.ParentId == parentId && x.Name.ToUpper() == normalized))
            throw new BusinessRuleException("A category with this name already exists at that level.");
    }

    private async Task<bool> IsDescendantAsync(int ancestorId, int candidateId)
    {
        var parent = await repository.Query.Where(x => x.Id == candidateId)
            .Select(x => x.ParentId).FirstOrDefaultAsync();
        while (parent.HasValue)
        {
            if (parent.Value == ancestorId) return true;
            parent = await repository.Query.Where(x => x.Id == parent.Value)
                .Select(x => x.ParentId).FirstOrDefaultAsync();
        }
        return false;
    }

    private static void Apply(Category category, CategoryWriteRequest request)
    {
        category.Name = request.Name.Trim();
        category.ParentId = request.ParentId;
        category.Description = request.Description.Trim();
        category.ImageUrl = request.ImageUrl?.Trim();
    }

    private async Task QueueCloudinaryDeletionAsync(string? publicId)
    {
        if (!string.IsNullOrWhiteSpace(publicId))
            await imageDeletionJobs.AddAsync(new ProductImageDeletionJob { PublicId = publicId });
    }

    private IQueryable<Category> CategoriesWithProducts() => repository.Query.AsNoTracking()
        .Include(x => x.Products).ThenInclude(x => x.Reviews)
        .Include(x => x.Products).ThenInclude(x => x.Images)
        .AsSplitQuery();

    private async Task<List<CategoryDto>> BuildTreeAsync(IEnumerable<Category> categories, IEnumerable<int> rootIds)
    {
        var categoryList = categories.ToList();
        var categoryProducts = categoryList.SelectMany(x => x.Products).DistinctBy(x => x.Id).ToList();
        var discounts = await pricing.GetDiscountsAsync(categoryProducts);
        var categoryDtos = categoryList.ToDictionary(x => x.Id, category => ToDto(category, discounts));

        foreach (var category in categoryList)
        {
            if (category.ParentId is int parentId && categoryDtos.TryGetValue(parentId, out var parent))
                parent.Children.Add(categoryDtos[category.Id]);
        }

        foreach (var category in categoryDtos.Values)
        {
            category.Children = category.Children.OrderBy(child => child.Id).ToList();
            category.Products = category.Products.OrderBy(product => product.Id).ToList();
        }

        return rootIds.Order().Select(id => categoryDtos[id]).ToList();
    }

    private static CategoryDto ToDto(Category category, IReadOnlyDictionary<int, decimal> discounts) => new()
    {
        Id = category.Id,
        Name = category.Name,
        ParentId = category.ParentId,
        Description = category.Description,
        ImageUrl = category.ImageUrl,
        Products = category.Products.OrderBy(product => product.Id).Select(product =>
            ProductService.ToDto(product, discounts[product.Id])).ToList()
    };
}
