using System.Data;
using BizSpace.Dtos;
using BizSpace.Exceptions;
using BizSpace.Models;
using BizSpace.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Repositories.Services;

public sealed class StoreService(
    IGenericRepository<Store> stores,
    IGenericRepository<Product> products,
    IGenericRepository<CartItem> cartItems,
    IGenericRepository<OrderItem> orderItems,
    IGenericRepository<ProductImage> productImages,
    IGenericRepository<ProductImageDeletionJob> imageDeletionJobs,
    IGenericRepository<MarketingCampaign> campaigns,
    IGenericRepository<MarketingCampaignProduct> campaignProducts,
    IUnitOfWork unitOfWork,
    INotificationService notifications) : IStoreService
{
    public async Task<List<StoreDto>> GetAllAsync() =>
        (await stores.Query.AsNoTracking().Where(x => x.ManagerId != null)
            .OrderBy(x => x.Id).ToListAsync()).Select(ToDto).ToList();

    public async Task<StoreDto> GetByIdAsync(int id)
    {
        var store = await stores.Query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.ManagerId != null)
            ?? throw new KeyNotFoundException("Store was not found.");
        return ToDto(store);
    }

    public async Task<List<StoreDto>> GetMineAsync(int managerId) =>
        (await stores.Query.AsNoTracking().Where(x => x.ManagerId == managerId)
            .OrderBy(x => x.Id).ToListAsync()).Select(ToDto).ToList();

    public async Task<StoreDto> CreateAsync(int managerId, CreateStoreRequest request)
    {
        var name = request.Name.Trim();
        var normalizedName = name.ToUpperInvariant();
        if (await stores.Query.AnyAsync(x => x.ManagerId == managerId && x.Name.ToUpper() == normalizedName))
            throw new BusinessRuleException("You already have a store with this name.");

        var store = new Store
        {
            ManagerId = managerId,
            Name = name,
            Description = request.Description.Trim()
        };
        await stores.AddAsync(store);
        await stores.SaveAsync();
        await notifications.NotifyAdminsAsync(NotificationType.StoreCreated, "Store created",
            $"{store.Name} was created by store manager {managerId}.", store.Id, store.Id);
        return ToDto(store);
    }

    public async Task DeleteAsync(int id, int callerId, bool isAdmin)
    {
        await using var transaction = unitOfWork.BeginTransaction(IsolationLevel.Serializable);
        var store = await stores.Query.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Store was not found.");
        if (!isAdmin && store.ManagerId != callerId)
            throw new ForbiddenException("You can delete only your own stores.");

        var storeProducts = await products.Query.Where(x => x.StoreId == id).ToListAsync();
        var productIds = storeProducts.Select(x => x.Id).ToList();
        var storeImages = await productImages.Query
            .Where(x => productIds.Contains(x.ProductId) && x.PublicId != null)
            .ToListAsync();
        var storeCartItems = await cartItems.Query.Where(x => x.Product != null && x.Product.StoreId == id).ToListAsync();
        var storeOrderItems = await orderItems.Query.Where(x => x.Product != null && x.Product.StoreId == id).ToListAsync();
        var storeCampaignLinks = await campaignProducts.Query
            .Where(x => x.Product != null && x.Product.StoreId == id).ToListAsync();
        var affectedCampaignIds = storeCampaignLinks.Select(x => x.CampaignId).Distinct().ToList();
        var productCampaignsWithoutRemainingProducts = await campaigns.Query
            .Where(x => affectedCampaignIds.Contains(x.Id) &&
                !campaignProducts.Query.Any(link => link.CampaignId == x.Id && link.Product != null && link.Product.StoreId != id))
            .ToListAsync();
        var storeCampaigns = await campaigns.Query.Where(x => x.StoreId == id).ToListAsync();
        var campaignsToDelete = storeCampaigns.Concat(productCampaignsWithoutRemainingProducts)
            .DistinctBy(x => x.Id).ToList();

        foreach (var image in storeImages)
            await imageDeletionJobs.AddAsync(new ProductImageDeletionJob { PublicId = image.PublicId! });

        cartItems.DeleteRange(storeCartItems);
        campaignProducts.DeleteRange(storeCampaignLinks);
        campaigns.DeleteRange(campaignsToDelete);
        foreach (var orderItem in storeOrderItems)
            orderItem.ProductId = null;
        // Detach order references and remove dependents before deleting products. Order-line snapshots remain intact.
        await unitOfWork.SaveChangesAsync();

        products.DeleteRange(storeProducts);
        await unitOfWork.SaveChangesAsync();

        stores.Delete(store);
        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static StoreDto ToDto(Store store) => new()
    {
        Id = store.Id,
        ManagerId = store.ManagerId,
        Name = store.Name,
        Description = store.Description,
        CreatedAtUtc = store.CreatedAtUtc
    };
}
