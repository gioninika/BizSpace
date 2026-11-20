using BizSpace.Controllers;
using BizSpace.Data;
using BizSpace.Dtos;
using BizSpace.Models;
using BizSpace.Repositories;
using BizSpace.Repositories.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BizSpace.Tests;

public sealed class ApiOrderingTests
{
    [Fact]
    public async Task ProductList_IsSortedByIdBeforePaginationAndPriceFiltering()
    {
        await using var db = await CreateDatabaseAsync();
        var service = CreateProductService(db);

        var firstPage = await service.GetAllAsync(new ProductQuery { PageSize = 2 });
        var secondPage = await service.GetAllAsync(new ProductQuery { Page = 2, PageSize = 2 });
        var priceFiltered = await service.GetAllAsync(new ProductQuery { MinPrice = 60m });

        Assert.Equal([4, 12], firstPage.Items.Select(product => product.Id));
        Assert.Equal([30], secondPage.Items.Select(product => product.Id));
        Assert.Equal([12, 30], priceFiltered.Items.Select(product => product.Id));
    }

    [Fact]
    public async Task CategoryTree_IsSortedByIdAtEachLevel()
    {
        await using var db = await CreateDatabaseAsync();
        var service = new CategoryService(
            new GenericRepository<Category>(db),
            new GenericRepository<ProductImageDeletionJob>(db),
            new TestPricingService(),
            imageStorage: null!,
            NullLogger<CategoryService>.Instance);

        var categories = await service.GetAllAsync();

        Assert.Equal([2, 9], categories.Select(category => category.Id));
        Assert.Equal([5], categories[0].Children.Select(category => category.Id));
        Assert.Equal([4], categories[0].Children[0].Products.Select(product => product.Id));
        Assert.Equal([14], categories[1].Children.Select(category => category.Id));
        Assert.Equal([12, 30], categories[1].Children[0].Products.Select(product => product.Id));
    }

    [Fact]
    public async Task StoreLists_AreSortedById()
    {
        await using var db = await CreateDatabaseAsync();
        var service = new StoreService(
            new GenericRepository<Store>(db),
            products: null!, cartItems: null!, orderItems: null!, productImages: null!,
            imageDeletionJobs: null!, campaigns: null!, campaignProducts: null!,
            unitOfWork: null!, notifications: null!);

        var allStores = await service.GetAllAsync();
        var managerStores = await service.GetMineAsync(managerId: 1);

        Assert.Equal([3, 12], allStores.Select(store => store.Id));
        Assert.Equal([3, 12], managerStores.Select(store => store.Id));
    }

    [Fact]
    public async Task OrderListsAndLines_AreSortedById()
    {
        await using var db = await CreateDatabaseAsync();
        var service = new OrderService(
            new GenericRepository<Order>(db),
            carts: null!, cartItems: null!, addresses: null!, unitOfWork: null!,
            notifications: null!, pricing: null!);

        var orders = await service.GetAllAsync(userId: 2, isAdmin: true);

        Assert.Equal([2, 15], orders.Select(order => order.Id));
        Assert.Equal([4, 30], orders[0].Items.Select(item => item.ProductId));
    }

    [Fact]
    public async Task Reviews_AreSortedById()
    {
        await using var db = await CreateDatabaseAsync();
        var service = new ProductReviewService(
            new GenericRepository<Product>(db),
            new GenericRepository<ProductReview>(db),
            new GenericRepository<UserAccount>(db));

        var reviews = await service.GetForProductAsync(productId: 4);

        Assert.Equal([2, 18], reviews.Select(review => review.Id));
    }

    [Fact]
    public async Task Addresses_AreSortedById()
    {
        await using var db = await CreateDatabaseAsync();
        var service = new AddressBookService(new GenericRepository<SavedAddress>(db));

        var addresses = await service.GetMineAsync(userId: 2);

        Assert.Equal([3, 20], addresses.Select(address => address.Id));
    }

    [Fact]
    public async Task Notifications_AreReturnedInIdOrder()
    {
        await using var db = await CreateDatabaseAsync();
        var service = new NotificationService(
            new GenericRepository<Notification>(db),
            users: null!, stores: null!, hub: null!, NullLogger<NotificationService>.Instance);

        var notifications = await service.GetMineAsync(userId: 2, unreadOnly: false);

        Assert.Equal([4, 17], notifications.Select(notification => notification.Id));
    }

    [Fact]
    public async Task Campaigns_AreSortedByIdAndProductIdsAreSorted()
    {
        await using var db = await CreateDatabaseAsync();
        var service = new MarketingCampaignService(
            new GenericRepository<MarketingCampaign>(db),
            new GenericRepository<Store>(db),
            new GenericRepository<Category>(db),
            new GenericRepository<Product>(db));

        var campaigns = await service.GetAllAsync(managerId: 1);

        Assert.Equal([3, 12], campaigns.Select(campaign => campaign.Id));
        Assert.Equal([4, 30], campaigns[1].ProductIds);
    }

    [Fact]
    public async Task CartAndWishlistLists_AreSortedByTheirDisplayedIds()
    {
        await using var db = await CreateDatabaseAsync();
        var pricing = new TestPricingService();
        var cartService = new CartService(
            new GenericRepository<Cart>(db),
            new GenericRepository<CartItem>(db),
            new GenericRepository<Product>(db),
            pricing);
        var wishlistService = new WishlistService(
            new GenericRepository<WishlistItem>(db),
            new GenericRepository<Product>(db),
            pricing);

        var cart = await cartService.GetMineAsync(userId: 2);
        var wishlist = await wishlistService.GetMineAsync(userId: 2);

        Assert.Equal([3, 22], cart.Items.Select(item => item.Id));
        Assert.Equal([4, 30], wishlist.Select(item => item.ProductId));
    }

    private static ProductService CreateProductService(StoreDbContext db) => new(
        new GenericRepository<Product>(db),
        new GenericRepository<Category>(db),
        new GenericRepository<Store>(db),
        new GenericRepository<ProductImage>(db),
        new GenericRepository<ProductImageDeletionJob>(db),
        notifications: null!,
        new TestPricingService(),
        imageStorage: null!,
        NullLogger<ProductService>.Instance);

    private static async Task<StoreDbContext> CreateDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<StoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new StoreDbContext(options);

        db.Users.AddRange(
            new UserAccount { Id = 1, DisplayName = "Manager", Email = "manager@example.test", Role = UserRoles.StoreManager },
            new UserAccount { Id = 2, DisplayName = "Customer", Email = "customer@example.test" });
        db.Stores.AddRange(
            new Store { Id = 12, ManagerId = 1, Name = "A Store" },
            new Store { Id = 3, ManagerId = 1, Name = "Z Store" });
        db.Categories.AddRange(
            new Category { Id = 9, Name = "A Root" },
            new Category { Id = 2, Name = "Z Root" },
            new Category { Id = 14, ParentId = 9, Name = "A Child" },
            new Category { Id = 5, ParentId = 2, Name = "Z Child" });
        await db.SaveChangesAsync();

        db.Products.AddRange(
            new Product { Id = 30, Name = "Alpha Product", Brand = "Brand", Description = "Description", Price = 100m, StoreId = 12, CategoryId = 14 },
            new Product { Id = 4, Name = "Zulu Product", Brand = "Brand", Description = "Description", Price = 50m, StoreId = 3, CategoryId = 5 },
            new Product { Id = 12, Name = "Middle Product", Brand = "Brand", Description = "Description", Price = 75m, StoreId = 3, CategoryId = 14 });
        db.ProductImages.AddRange(
            new ProductImage { Id = 8, ProductId = 4, SecureUrl = "https://example.test/second.jpg", SortOrder = 1 },
            new ProductImage { Id = 40, ProductId = 4, SecureUrl = "https://example.test/primary.jpg", SortOrder = 0 });
        db.SavedAddresses.AddRange(
            new SavedAddress { Id = 20, UserId = 2, Label = "Older ID" },
            new SavedAddress { Id = 3, UserId = 2, Label = "Newer ID" });
        db.Notifications.AddRange(
            new Notification { Id = 17, RecipientUserId = 2, Title = "Older ID", Message = "Message", CreatedAtUtc = DateTime.UtcNow.AddDays(-2) },
            new Notification { Id = 4, RecipientUserId = 2, Title = "Newer ID", Message = "Message", CreatedAtUtc = DateTime.UtcNow });
        db.ProductReviews.AddRange(
            new ProductReview { Id = 18, ProductId = 4, UserId = 2, Rating = 5, Comment = "Older ID", CreatedAtUtc = DateTime.UtcNow.AddDays(-2) },
            new ProductReview { Id = 2, ProductId = 4, UserId = 2, Rating = 4, Comment = "Newer ID", CreatedAtUtc = DateTime.UtcNow });

        var firstOrder = new Order
        {
            Id = 15,
            UserId = 2,
            ShippingAddress = new ShippingAddress(),
            PaymentMethod = "Cash",
            DeliveryMethod = "Pickup",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
            Items = [new OrderItem { Id = 3, ProductId = 12, ProductName = "Middle Product", Quantity = 1, Price = 75m }]
        };
        var secondOrder = new Order
        {
            Id = 2,
            UserId = 2,
            ShippingAddress = new ShippingAddress(),
            PaymentMethod = "Cash",
            DeliveryMethod = "Pickup",
            CreatedAtUtc = DateTime.UtcNow,
            Items =
            [
                new OrderItem { Id = 31, ProductId = 30, ProductName = "Alpha Product", Quantity = 1, Price = 100m },
                new OrderItem { Id = 7, ProductId = 4, ProductName = "Zulu Product", Quantity = 1, Price = 50m }
            ]
        };
        db.Orders.AddRange(firstOrder, secondOrder);

        var firstCampaign = new MarketingCampaign
        {
            Id = 12, StoreId = 12, Name = "A Campaign", Description = "Description", DiscountPercent = 10,
            StartsAtUtc = DateTime.UtcNow, EndsAtUtc = DateTime.UtcNow.AddDays(1),
            Products = [new MarketingCampaignProduct { ProductId = 30 }, new MarketingCampaignProduct { ProductId = 4 }]
        };
        var secondCampaign = new MarketingCampaign
        {
            Id = 3, StoreId = 3, Name = "Z Campaign", Description = "Description", DiscountPercent = 10,
            StartsAtUtc = DateTime.UtcNow, EndsAtUtc = DateTime.UtcNow.AddDays(1)
        };
        db.MarketingCampaigns.AddRange(firstCampaign, secondCampaign);

        var cart = new Cart
        {
            Id = 1,
            UserId = 2,
            Items =
            [
                new CartItem { Id = 22, ProductId = 30, Quantity = 1 },
                new CartItem { Id = 3, ProductId = 4, Quantity = 1 }
            ]
        };
        db.Carts.Add(cart);
        db.WishlistItems.AddRange(
            new WishlistItem { Id = 25, UserId = 2, ProductId = 30 },
            new WishlistItem { Id = 2, UserId = 2, ProductId = 4 });

        await db.SaveChangesAsync();
        return db;
    }

    private sealed class TestPricingService : IPricingService
    {
        public Task<Dictionary<int, decimal>> GetDiscountsAsync(IReadOnlyCollection<Product> products) =>
            Task.FromResult(products.ToDictionary(product => product.Id, product => product.DiscountPercent));
    }
}
