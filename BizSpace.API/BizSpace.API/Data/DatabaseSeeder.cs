using BizSpace.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Data;

public static class DatabaseSeeder
{
    private const string ManagerOneEmail = "nino.manager@example.com";
    private const string ManagerTwoEmail = "giorgi.manager@example.com";
    private const string ManagerOnePassword = "NinoDemo-2026!Orbit#73";
    private const string ManagerTwoPassword = "GiorgiDemo-2026!Glow#48";

    private static readonly CategorySeed[] CategorySeeds =
    [
        new("Electronics & Accessories", "Consumer electronics and everyday tech accessories."),
        new("Books & Stationery", "Books, learning materials, and writing supplies."),
        new("Home & Kitchen", "Useful products for cooking, lighting, and daily living."),
        new("Fashion & Accessories", "Clothing, bags, footwear, and personal accessories."),
        new("Beauty & Personal Care", "Skincare, makeup, haircare, and personal wellness."),
        new("Sports & Outdoors", "Fitness, outdoor recreation, and sporting goods."),
        new("Toys & Baby", "Toys, baby care, and nursery essentials."),
        new("Grocery & Gourmet", "Pantry goods, snacks, and specialty beverages."),
        new("Pet Supplies", "Food, toys, grooming, and accessories for pets."),
        new("Automotive", "Vehicle care and practical automotive accessories."),
        new("Garden & Outdoor Living", "Plants, garden tools, and outdoor furniture.")
    ];

    private static readonly CategorySeed[] SubcategorySeeds =
    [
        new("Audio", "Speakers, headphones, and personal audio devices.", "Electronics & Accessories"),
        new("Wearables", "Smartwatches and other wearable technology.", "Electronics & Accessories"),
        new("Phone Accessories", "Chargers, cases, cables, and mobile accessories.", "Electronics & Accessories"),
        new("Computers", "Computers and computer accessories.", "Electronics & Accessories"),
        new("Fiction", "Novels and other works of fiction.", "Books & Stationery"),
        new("Education", "Educational books and learning materials.", "Books & Stationery"),
        new("Office Supplies", "Stationery and supplies for work and study.", "Books & Stationery"),
        new("Drinkware", "Reusable bottles, tumblers, and cups.", "Home & Kitchen"),
        new("Lighting", "Desk lamps and lighting for indoor spaces.", "Home & Kitchen"),
        new("Coffee & Tea", "Coffee, tea, and home brewing equipment.", "Home & Kitchen"),
        new("Cookware", "Pots, pans, utensils, and cooking tools.", "Home & Kitchen"),
        new("Bags", "Everyday bags, totes, and backpacks.", "Fashion & Accessories"),
        new("Footwear", "Casual and formal footwear.", "Fashion & Accessories"),
        new("Wallets", "Wallets and small leather goods.", "Fashion & Accessories"),
        new("Clothing", "Clothing for everyday wear.", "Fashion & Accessories"),
        new("Skincare", "Serums, cleansers, moisturizers, and skincare essentials.", "Beauty & Personal Care"),
        new("Makeup", "Cosmetics and makeup accessories.", "Beauty & Personal Care"),
        new("Haircare", "Shampoo, conditioner, and hair styling products.", "Beauty & Personal Care"),
        new("Fitness", "Training accessories and fitness equipment.", "Sports & Outdoors"),
        new("Camping & Hiking", "Equipment for camping, hiking, and travel.", "Sports & Outdoors"),
        new("Cycling", "Bicycles and cycling accessories.", "Sports & Outdoors"),
        new("Toys", "Games, toys, and creative play products.", "Toys & Baby"),
        new("Baby Care", "Everyday care products for babies.", "Toys & Baby"),
        new("Nursery", "Furniture and supplies for nurseries.", "Toys & Baby"),
        new("Pantry", "Shelf-stable foods and cooking ingredients.", "Grocery & Gourmet"),
        new("Snacks", "Packaged snacks and treats.", "Grocery & Gourmet"),
        new("Beverages", "Coffee, tea, and other drinks.", "Grocery & Gourmet"),
        new("Pet Food", "Food and treats for pets.", "Pet Supplies"),
        new("Pet Toys & Accessories", "Toys, collars, and everyday pet accessories.", "Pet Supplies"),
        new("Pet Grooming", "Grooming products and tools for pets.", "Pet Supplies"),
        new("Car Care", "Cleaning and maintenance products for vehicles.", "Automotive"),
        new("Auto Accessories", "Useful accessories for cars and other vehicles.", "Automotive"),
        new("Plants", "Indoor and outdoor plants and planters.", "Garden & Outdoor Living"),
        new("Garden Tools", "Tools and supplies for gardening.", "Garden & Outdoor Living"),
        new("Outdoor Furniture", "Furniture and accessories for patios and gardens.", "Garden & Outdoor Living")
    ];

    private static readonly StoreSeed[] StoreSeeds =
    [
        new("Orbit Tech", "Everyday electronics, audio, and wearable technology.", ManagerOneEmail),
        new("Hearth & Cup", "Thoughtful home, lighting, and coffee essentials.", ManagerOneEmail),
        new("Thread & Trail", "Practical fashion and accessories for everyday use.", ManagerTwoEmail),
        new("Glow Studio", "A curated collection of skincare and personal care.", ManagerTwoEmail)
    ];

    private static readonly ProductSeed[] ProductSeeds =
    [
        new("JBL Go 4 Portable Bluetooth Speaker", "JBL", "Compact portable speaker with Bluetooth connectivity and a durable everyday design.", 149.00m, 10m, "Orbit Tech", "Electronics & Accessories", "Audio", 3, 5),
        new("Tempo Active Smartwatch", "Tempo", "Lightweight smartwatch for activity tracking, notifications, and daily wear.", 329.00m, 0m, "Orbit Tech", "Electronics & Accessories", "Wearables", 18, 4),
        new("EchoBud True Wireless Earbuds", "EchoBud", "Wireless in-ear earbuds with a compact charging case and touch controls.", 189.00m, 5m, "Orbit Tech", "Electronics & Accessories", "Audio", 22, 5),
        new("Terra Steel Travel Tumbler", "Terra", "Reusable insulated stainless-steel tumbler for hot and cold drinks.", 59.00m, 0m, "Hearth & Cup", "Home & Kitchen", "Drinkware", 36, 8),
        new("Luma Arc Desk Lamp", "Luma", "Minimal adjustable desk lamp for reading, study, and home-office lighting.", 139.00m, 0m, "Hearth & Cup", "Home & Kitchen", "Lighting", 11, 3),
        new("BrewCraft Pour-Over Coffee Set", "BrewCraft", "Glass coffee server and reusable dripper set for manual pour-over brewing.", 119.00m, 10m, "Hearth & Cup", "Home & Kitchen", "Coffee & Tea", 14, 4),
        new("Everyday Canvas Tote", "Threadline", "Durable natural-canvas tote bag with reinforced handles for everyday use.", 45.00m, 0m, "Thread & Trail", "Fashion & Accessories", "Bags", 28, 6),
        new("Cloudstep White Sneakers", "Cloudstep", "Clean low-top sneakers with a cushioned footbed and versatile styling.", 219.00m, 15m, "Thread & Trail", "Fashion & Accessories", "Footwear", 7, 5),
        new("Classic Leather Wallet", "Northline", "Slim leather wallet with practical card slots and a compact profile.", 89.00m, 0m, "Thread & Trail", "Fashion & Accessories", "Wallets", 16, 4),
        new("C-Glow Vitamin C Serum", "C-Glow", "Lightweight vitamin C facial serum in a glass dropper bottle.", 79.00m, 0m, "Glow Studio", "Beauty & Personal Care", "Skincare", 20, 5),
        new("Dewdrop Face Moisturizer", "Dewdrop", "Daily face moisturizer with a smooth texture for a simple skincare routine.", 69.00m, 0m, "Glow Studio", "Beauty & Personal Care", "Skincare", 24, 5)
    ];

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var db = services.GetRequiredService<StoreDbContext>();
        await SeedCategoriesAsync(db);

        var managerOne = await EnsureManagerAsync(db, ManagerOneEmail, "Nino Kapanadze", ManagerOnePassword);
        var managerTwo = await EnsureManagerAsync(db, ManagerTwoEmail, "Giorgi Beridze", ManagerTwoPassword);
        var stores = await SeedStoresAsync(db, managerOne, managerTwo);
        await SeedProductsAsync(db, stores);
        await SeedDemoImagesAsync(db);
        await SeedAdminAsync(db, configuration);
    }

    private static async Task SeedCategoriesAsync(StoreDbContext db)
    {
        var categories = await db.Categories.ToListAsync();
        var roots = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);

        foreach (var seed in CategorySeeds)
        {
            var existing = categories.FirstOrDefault(category => category.ParentId is null &&
                string.Equals(category.Name, seed.Name, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                existing = new Category { Name = seed.Name, Description = seed.Description };
                db.Categories.Add(existing);
                categories.Add(existing);
            }
            roots[seed.Name] = existing;
        }

        await db.SaveChangesAsync();

        foreach (var seed in SubcategorySeeds)
        {
            var parent = roots[seed.ParentName!];
            if (categories.Any(category => category.ParentId == parent.Id &&
                string.Equals(category.Name, seed.Name, StringComparison.OrdinalIgnoreCase)))
                continue;

            var category = new Category
            {
                Name = seed.Name,
                Description = seed.Description,
                ParentId = parent.Id
            };
            db.Categories.Add(category);
            categories.Add(category);
        }

        await db.SaveChangesAsync();
    }

    private static async Task<UserAccount> EnsureManagerAsync(
        StoreDbContext db,
        string email,
        string displayName,
        string password)
    {
        var manager = await db.Users.FirstOrDefaultAsync(user => user.Email == email);
        if (manager is not null)
        {
            if (!string.Equals(manager.Role, UserRoles.StoreManager, StringComparison.Ordinal))
                throw new InvalidOperationException($"The demo manager email {email} is already used by a non-manager account.");

            var hasher = new PasswordHasher<UserAccount>();
            manager.DisplayName = displayName;
            manager.EmailConfirmed = true;
            manager.EmailCodeHash = null;
            manager.EmailCodePurpose = null;
            manager.EmailCodeExpiresAtUtc = null;
            manager.EmailCodeSentAtUtc = null;
            manager.EmailCodeFailedAttempts = 0;
            if (string.IsNullOrWhiteSpace(manager.PasswordHash) ||
                hasher.VerifyHashedPassword(manager, manager.PasswordHash, password) == PasswordVerificationResult.Failed)
                manager.PasswordHash = hasher.HashPassword(manager, password);

            await db.SaveChangesAsync();
            return manager;
        }

        manager = new UserAccount
        {
            DisplayName = displayName,
            Email = email,
            Role = UserRoles.StoreManager,
            EmailConfirmed = true
        };
        manager.PasswordHash = new PasswordHasher<UserAccount>().HashPassword(manager, password);
        db.Users.Add(manager);
        await db.SaveChangesAsync();
        return manager;
    }

    private static async Task<Dictionary<string, Store>> SeedStoresAsync(
        StoreDbContext db,
        UserAccount managerOne,
        UserAccount managerTwo)
    {
        var managers = new Dictionary<string, UserAccount>(StringComparer.OrdinalIgnoreCase)
        {
            [ManagerOneEmail] = managerOne,
            [ManagerTwoEmail] = managerTwo
        };
        var stores = new Dictionary<string, Store>(StringComparer.OrdinalIgnoreCase);

        foreach (var seed in StoreSeeds)
        {
            var manager = managers[seed.ManagerEmail];
            var store = await db.Stores.FirstOrDefaultAsync(candidate =>
                candidate.ManagerId == manager.Id && candidate.Name == seed.Name);
            if (store is null)
            {
                store = new Store
                {
                    Name = seed.Name,
                    Description = seed.Description,
                    ManagerId = manager.Id
                };
                db.Stores.Add(store);
            }

            stores[seed.Name] = store;
        }

        await db.SaveChangesAsync();
        return stores;
    }

    private static async Task SeedProductsAsync(StoreDbContext db, IReadOnlyDictionary<string, Store> stores)
    {
        var categories = await db.Categories.AsNoTracking().ToListAsync();
        foreach (var seed in ProductSeeds)
        {
            var store = stores[seed.StoreName];
            var root = categories.First(category => category.ParentId is null &&
                string.Equals(category.Name, seed.CategoryRootName, StringComparison.OrdinalIgnoreCase));
            var category = categories.First(candidate => candidate.ParentId == root.Id &&
                string.Equals(candidate.Name, seed.CategoryName, StringComparison.OrdinalIgnoreCase));

            var exists = await db.Products.AnyAsync(product => product.StoreId == store.Id && product.Name == seed.Name);
            if (exists)
                continue;

            db.Products.Add(new Product
            {
                Name = seed.Name,
                Brand = seed.Brand,
                Description = seed.Description,
                Price = seed.Price,
                DiscountPercent = seed.DiscountPercent,
                StoreId = store.Id,
                CategoryId = category.Id,
                Stock = seed.Stock,
                MinimumStockLevel = seed.MinimumStockLevel
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedDemoImagesAsync(StoreDbContext db)
    {
        foreach (var (productName, images) in DemoCatalogImages.ProductImages)
        {
            var product = await db.Products.FirstOrDefaultAsync(candidate => candidate.Name == productName);
            if (product is null)
                continue;

            var existingImages = await db.ProductImages
                .Where(image => image.ProductId == product.Id)
                .ToListAsync();

            if (existingImages.Count == 0)
            {
                foreach (var image in images)
                {
                    db.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        PublicId = image.SecureUrl.StartsWith("/", StringComparison.Ordinal)
                            ? null
                            : image.PublicId,
                        SecureUrl = image.SecureUrl,
                        Format = image.Format,
                        Width = image.Width,
                        Height = image.Height,
                        SortOrder = image.SortOrder
                    });
                }

                continue;
            }

            foreach (var image in images)
            {
                var existing = existingImages.FirstOrDefault(candidate =>
                    string.Equals(candidate.PublicId, image.PublicId, StringComparison.Ordinal) ||
                    (string.IsNullOrWhiteSpace(candidate.PublicId) &&
                     string.Equals(candidate.SecureUrl, image.SecureUrl, StringComparison.Ordinal)));
                if (existing is null)
                    continue;

                existing.PublicId = image.SecureUrl.StartsWith("/", StringComparison.Ordinal)
                    ? null
                    : image.PublicId;
                existing.SecureUrl = image.SecureUrl;
                existing.Format = image.Format;
                existing.Width = image.Width;
                existing.Height = image.Height;
            }
        }

        foreach (var (categoryName, image) in DemoCatalogImages.CategoryImages)
        {
            var category = await db.Categories.FirstOrDefaultAsync(candidate =>
                candidate.ParentId == null && candidate.Name == categoryName);
            if (category is null || !string.IsNullOrWhiteSpace(category.ImageUrl))
                continue;

            category.ImageUrl = image.SecureUrl;
            category.ImagePublicId = image.PublicId;
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedAdminAsync(StoreDbContext db, IConfiguration configuration)
    {
        var email = configuration["SeedAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) ||
            await db.Users.AnyAsync(user => user.Email == email))
            return;

        var admin = new UserAccount
        {
            DisplayName = "Administrator",
            Email = email,
            Role = UserRoles.Admin,
            EmailConfirmed = true
        };
        admin.PasswordHash = new PasswordHasher<UserAccount>().HashPassword(admin, password);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }

    private sealed record CategorySeed(string Name, string Description, string? ParentName = null);
    private sealed record StoreSeed(string Name, string Description, string ManagerEmail);
    private sealed record ProductSeed(
        string Name,
        string Brand,
        string Description,
        decimal Price,
        decimal DiscountPercent,
        string StoreName,
        string CategoryRootName,
        string CategoryName,
        int Stock,
        int MinimumStockLevel);
}
