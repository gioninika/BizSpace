using BizSpace.Dtos;
using BizSpace.Exceptions;
using BizSpace.Models;
using BizSpace.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Repositories.Services;

public sealed class MarketingCampaignService(
    IGenericRepository<MarketingCampaign> campaigns,
    IGenericRepository<Store> stores,
    IGenericRepository<Category> categories,
    IGenericRepository<Product> products) : IMarketingCampaignService
{
    public async Task<List<MarketingCampaignDto>> GetAllAsync(int managerId) =>
        (await campaigns.Query.AsNoTracking().Include(x => x.Products).Where(x =>
            x.Store != null && x.Store.ManagerId == managerId ||
            x.Products.Any(link => link.Product != null && link.Product.Store != null &&
                link.Product.Store.ManagerId == managerId))
            .OrderBy(x => x.Id).ToListAsync())
            .Select(ToDto).ToList();

    public async Task<MarketingCampaignDto> CreateAsync(MarketingCampaignWriteRequest request, int managerId)
    {
        await ValidateAsync(request, managerId);
        var campaign = new MarketingCampaign();
        Apply(campaign, request);
        SetProducts(campaign, request.ProductIds);
        await campaigns.AddAsync(campaign);
        await campaigns.SaveAsync();
        return ToDto(campaign);
    }

    public async Task<MarketingCampaignDto> UpdateAsync(int id, MarketingCampaignWriteRequest request, int managerId)
    {
        await ValidateAsync(request, managerId);
        var campaign = await FindOwnedCampaignAsync(id, managerId);
        Apply(campaign, request);
        SetProducts(campaign, request.ProductIds);
        await campaigns.SaveAsync();
        return ToDto(campaign);
    }

    public async Task DeleteAsync(int id, int managerId)
    {
        var campaign = await FindOwnedCampaignAsync(id, managerId);
        campaigns.Delete(campaign);
        await campaigns.SaveAsync();
    }

    private async Task ValidateAsync(MarketingCampaignWriteRequest request, int managerId)
    {
        if (request.StartsAtUtc >= request.EndsAtUtc)
            throw new ArgumentException("Campaign end time must be later than its start time.");

        var hasProducts = request.ProductIds.Count > 0;
        var storeCampaign = request.StoreId.HasValue && !request.CategoryId.HasValue && !hasProducts;
        var categoryCampaign = request.StoreId.HasValue && request.CategoryId.HasValue && !hasProducts;
        var productCampaign = !request.StoreId.HasValue && !request.CategoryId.HasValue && hasProducts;
        if (!storeCampaign && !categoryCampaign && !productCampaign)
            throw new ArgumentException("A campaign must target one of your stores, a category in one of your stores, or one or more products in your stores.");

        if (request.StoreId.HasValue && !await stores.Query.AnyAsync(x => x.Id == request.StoreId.Value && x.ManagerId == managerId))
            throw new ForbiddenException("You can create campaigns only for your own stores.");
        if (request.CategoryId.HasValue && !await categories.Query.AnyAsync(x => x.Id == request.CategoryId.Value))
            throw new KeyNotFoundException("Category was not found.");
        if (request.ProductIds.Any(id => id <= 0))
            throw new ArgumentException("Product IDs must be positive integers.");
        if (request.ProductIds.Distinct().Count() != request.ProductIds.Count)
            throw new ArgumentException("Product IDs must not contain duplicates.");
        if (hasProducts)
        {
            var ownedProductCount = await products.Query.CountAsync(x =>
                request.ProductIds.Contains(x.Id) && x.Store!.ManagerId == managerId);
            if (ownedProductCount != request.ProductIds.Count)
                throw new ForbiddenException("You can create campaigns only for products in your stores.");
        }
    }

    private async Task<MarketingCampaign> FindOwnedCampaignAsync(int id, int managerId) =>
        await campaigns.Query.Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id &&
            (x.Store != null && x.Store.ManagerId == managerId ||
             x.Products.Any(link => link.Product != null && link.Product.Store != null &&
                 link.Product.Store.ManagerId == managerId)))
        ?? throw new KeyNotFoundException("Marketing campaign was not found in your stores.");

    private static void Apply(MarketingCampaign campaign, MarketingCampaignWriteRequest request)
    {
        campaign.Name = request.Name.Trim();
        campaign.Description = request.Description.Trim();
        campaign.DiscountPercent = request.DiscountPercent;
        campaign.StartsAtUtc = DateTime.SpecifyKind(request.StartsAtUtc, DateTimeKind.Utc);
        campaign.EndsAtUtc = DateTime.SpecifyKind(request.EndsAtUtc, DateTimeKind.Utc);
        campaign.IsEnabled = request.IsEnabled;
        campaign.StoreId = request.StoreId;
        campaign.CategoryId = request.CategoryId;
    }

    private static void SetProducts(MarketingCampaign campaign, IEnumerable<int> productIds)
    {
        campaign.Products.Clear();
        foreach (var productId in productIds)
            campaign.Products.Add(new MarketingCampaignProduct { Campaign = campaign, ProductId = productId });
    }

    private static MarketingCampaignDto ToDto(MarketingCampaign campaign)
    {
        var now = DateTime.UtcNow;
        return new MarketingCampaignDto
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            DiscountPercent = campaign.DiscountPercent,
            StartsAtUtc = campaign.StartsAtUtc,
            EndsAtUtc = campaign.EndsAtUtc,
            IsEnabled = campaign.IsEnabled,
            IsCurrentlyActive = campaign.IsEnabled && campaign.StartsAtUtc <= now && now < campaign.EndsAtUtc,
            StoreId = campaign.StoreId,
            CategoryId = campaign.CategoryId,
            ProductIds = campaign.Products.Select(link => link.ProductId).Order().ToList()
        };
    }
}

public sealed class PricingService(IGenericRepository<MarketingCampaign> campaigns) : IPricingService
{
    public async Task<Dictionary<int, decimal>> GetDiscountsAsync(IReadOnlyCollection<Product> products)
    {
        if (products.Count == 0) return [];
        var now = DateTime.UtcNow;
        var activeCampaigns = await campaigns.Query.AsNoTracking().Include(x => x.Products)
            .Where(x => x.IsEnabled && x.StartsAtUtc <= now && x.EndsAtUtc > now)
            .ToListAsync();
        var discounts = new Dictionary<int, decimal>(products.Count);

        foreach (var product in products)
        {
            var campaignDiscount = activeCampaigns
                .Where(x => !x.StoreId.HasValue && !x.CategoryId.HasValue && x.Products.Count == 0 ||
                    x.Products.Any(link => link.ProductId == product.Id) ||
                    x.StoreId == product.StoreId && (!x.CategoryId.HasValue || x.CategoryId == product.CategoryId))
                .Select(x => x.DiscountPercent)
                .DefaultIfEmpty(0)
                .Max();
            discounts[product.Id] = Math.Max(product.DiscountPercent, campaignDiscount);
        }

        return discounts;
    }
}
