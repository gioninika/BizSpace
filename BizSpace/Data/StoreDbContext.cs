using BizSpace.Models;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Data;

public class StoreDbContext(DbContextOptions<StoreDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SavedAddress> SavedAddresses => Set<SavedAddress>();
    public DbSet<MarketingCampaign> MarketingCampaigns => Set<MarketingCampaign>();
    public DbSet<MarketingCampaignProduct> MarketingCampaignProducts => Set<MarketingCampaignProduct>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductImageDeletionJob> ProductImageDeletionJobs => Set<ProductImageDeletionJob>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<UserAccount>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<UserAccount>().Property(x => x.EmailConfirmed).HasDefaultValue(true);
        modelBuilder.Entity<UserAccount>().Property(x => x.EmailCodeFailedAttempts).HasDefaultValue(0);
        modelBuilder.Entity<UserAccount>().Property(x => x.EmailCodeHash).HasMaxLength(128);
        modelBuilder.Entity<UserAccount>().Property(x => x.EmailCodePurpose).HasMaxLength(32);
        modelBuilder.Entity<Store>().Property(x => x.Name).HasMaxLength(160);
        modelBuilder.Entity<Store>().Property(x => x.Description).HasMaxLength(2000);
        modelBuilder.Entity<Store>().HasIndex(x => new { x.ManagerId, x.Name }).IsUnique();
        modelBuilder.Entity<Notification>().Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
        modelBuilder.Entity<Notification>().Property(x => x.Title).HasMaxLength(160);
        modelBuilder.Entity<Notification>().Property(x => x.Message).HasMaxLength(2000);
        modelBuilder.Entity<Notification>().HasIndex(x => new { x.RecipientUserId, x.CreatedAtUtc });
        modelBuilder.Entity<Notification>().HasIndex(x => x.StoreId);
        modelBuilder.Entity<SavedAddress>().Property(x => x.Label).HasMaxLength(80);
        modelBuilder.Entity<SavedAddress>().Property(x => x.StreetAddress).HasMaxLength(200);
        modelBuilder.Entity<SavedAddress>().Property(x => x.City).HasMaxLength(100);
        modelBuilder.Entity<SavedAddress>().Property(x => x.PostalCode).HasMaxLength(20);
        modelBuilder.Entity<SavedAddress>().Property(x => x.Country).HasMaxLength(100);
        modelBuilder.Entity<SavedAddress>().HasIndex(x => x.UserId);
        modelBuilder.Entity<MarketingCampaign>().Property(x => x.Name).HasMaxLength(160);
        modelBuilder.Entity<MarketingCampaign>().Property(x => x.Description).HasMaxLength(2000);
        modelBuilder.Entity<MarketingCampaign>().Property(x => x.DiscountPercent).HasPrecision(5, 2);
        modelBuilder.Entity<MarketingCampaign>().HasIndex(x => new { x.StartsAtUtc, x.EndsAtUtc, x.IsEnabled });
        modelBuilder.Entity<MarketingCampaign>().HasIndex(x => x.StoreId);
        modelBuilder.Entity<MarketingCampaign>().HasIndex(x => x.CategoryId);
        modelBuilder.Entity<MarketingCampaignProduct>().HasIndex(x => x.ProductId);
        modelBuilder.Entity<MarketingCampaignProduct>().HasKey(x => new { x.CampaignId, x.ProductId });
        modelBuilder.Entity<Product>().HasIndex(x => x.StoreId);
        modelBuilder.Entity<Product>().Property(x => x.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Product>().Property(x => x.DiscountPercent).HasPrecision(5, 2);
        modelBuilder.Entity<Product>().Property(x => x.Brand).HasMaxLength(100);
        modelBuilder.Entity<Product>().HasIndex(x => new { x.CategoryId, x.Price });
        modelBuilder.Entity<Order>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Order>().Property(x => x.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Order>().Property(x => x.PaymentMethod).HasMaxLength(80);
        modelBuilder.Entity<Order>().Property(x => x.DeliveryMethod).HasMaxLength(80);
        modelBuilder.Entity<OrderItem>().Property(x => x.Price).HasPrecision(18, 2);

        modelBuilder.Entity<ProductImage>().Property(x => x.PublicId).HasMaxLength(512);
        modelBuilder.Entity<ProductImage>().Property(x => x.Format).HasMaxLength(20);
        modelBuilder.Entity<ProductImage>().HasIndex(x => new { x.ProductId, x.SortOrder });
        modelBuilder.Entity<ProductImage>().HasOne(x => x.Product).WithMany(x => x.Images)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductImageDeletionJob>().Property(x => x.PublicId).HasMaxLength(512);
        modelBuilder.Entity<ProductImageDeletionJob>().Property(x => x.LastError).HasMaxLength(1000);
        modelBuilder.Entity<ProductImageDeletionJob>().HasIndex(x => x.CreatedAtUtc);
        modelBuilder.Entity<Category>().Property(x => x.ImagePublicId).HasMaxLength(512);

        modelBuilder.Entity<Category>().HasOne(x => x.Parent).WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Product>().HasOne(x => x.Category).WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Product>().HasOne(x => x.Store).WithMany(x => x.Products)
            .HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Store>().HasOne(x => x.Manager).WithMany(x => x.ManagedStores)
            .HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Notification>().HasOne(x => x.Recipient).WithMany(x => x.Notifications)
            .HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Notification>().HasOne(x => x.Sender).WithMany(x => x.SentNotifications)
            .HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Notification>().HasOne(x => x.Store).WithMany()
            .HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<SavedAddress>().HasOne(x => x.User).WithMany(x => x.Addresses)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MarketingCampaign>().HasOne(x => x.Store).WithMany(x => x.MarketingCampaigns)
            .HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MarketingCampaign>().HasOne(x => x.Category).WithMany(x => x.MarketingCampaigns)
            .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MarketingCampaignProduct>().HasOne(x => x.Campaign).WithMany(x => x.Products)
            .HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MarketingCampaignProduct>().HasOne(x => x.Product).WithMany(x => x.MarketingCampaigns)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Cart>().HasIndex(x => x.UserId).IsUnique();
        modelBuilder.Entity<CartItem>().HasIndex(x => new { x.CartId, x.ProductId }).IsUnique();
        modelBuilder.Entity<CartItem>().HasOne(x => x.Product).WithMany(x => x.CartItems).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>().OwnsOne(x => x.ShippingAddress);
        modelBuilder.Entity<OrderItem>().HasOne(x => x.Product).WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<WishlistItem>().HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
        modelBuilder.Entity<ProductReview>().HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
        modelBuilder.Entity<ProductReview>().ToTable(table => table.HasCheckConstraint("CK_ProductReviews_Rating", "[Rating] >= 1 AND [Rating] <= 5"));
    }
}
