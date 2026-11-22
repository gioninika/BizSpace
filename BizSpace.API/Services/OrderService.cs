using System.Data;
using BizSpace.Dtos;
using BizSpace.Exceptions;
using BizSpace.Models;
using BizSpace.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Repositories.Services;

public sealed class OrderService(
    IGenericRepository<Order> orders,
    IGenericRepository<Cart> carts,
    IGenericRepository<CartItem> cartItems,
    IGenericRepository<SavedAddress> addresses,
    IUnitOfWork unitOfWork,
    INotificationService notifications,
    IPricingService pricing) : IOrderService
{
    public async Task<List<OrderDto>> GetAllAsync(int userId, bool isAdmin)
    {
        IQueryable<Order> query = orders.Query.AsNoTracking().Include(x => x.User)
            .Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x!.Images);
        if (!isAdmin) query = query.Where(x => x.UserId == userId);
        var orderList = await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).ToListAsync();
        return orderList.Select(order => ToDto(order)).ToList();
    }

    public async Task<OrderDto> GetByIdAsync(int id, int userId, bool isAdmin)
    {
        var order = await orders.Query.AsNoTracking().Include(x => x.User)
            .Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x!.Images)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Order was not found.");
        if (!isAdmin && order.UserId != userId)
            throw new ForbiddenException("You cannot access this order.");
        return ToDto(order);
    }

    public async Task<List<OrderDto>> GetForStoreManagerAsync(int managerId)
    {
        var managerOrders = await orders.Query.AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x!.Store)
            .Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x!.Images)
            .Where(x => x.Items.Any(item => item.Product != null && item.Product.Store != null &&
                item.Product.Store.ManagerId == managerId))
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .ToListAsync();
        return managerOrders.Select(order => ToDto(order, managerId)).ToList();
    }

    public async Task<OrderDto> GetForStoreManagerAsync(int id, int managerId)
    {
        var order = await orders.Query.AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x!.Store)
            .Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x!.Images)
            .FirstOrDefaultAsync(x => x.Id == id && x.Items.Any(item => item.Product != null &&
                item.Product.Store != null && item.Product.Store.ManagerId == managerId))
            ?? throw new KeyNotFoundException("Order was not found.");
        return ToDto(order, managerId);
    }

    public async Task<OrderDto> CreateFromCartAsync(
        int userId,
        CreateOrderRequest request,
        PaymentMethod paymentMethod,
        DeliveryMethod deliveryMethod)
    {
        var shippingAddress = await ResolveShippingAddressAsync(userId, request, deliveryMethod);
        await using var transaction = unitOfWork.BeginTransaction(IsolationLevel.Serializable);
        var cart = await carts.Query.Include(x => x.Items).ThenInclude(x => x.Product)
            .ThenInclude(x => x!.Store)
            .FirstOrDefaultAsync(x => x.UserId == userId)
            ?? throw new BusinessRuleException("Your cart is empty.");
        if (cart.Items.Count == 0)
            throw new BusinessRuleException("Your cart is empty.");

        var productDiscounts = await pricing.GetDiscountsAsync(cart.Items
            .Where(x => x.Product is not null).Select(x => x.Product!).ToList());
        var orderItems = new List<OrderItem>();
        var lowStockProducts = new List<(int StoreId, int ProductId, string ProductName, int Stock, int MinimumStockLevel)>();
        var storeIds = new HashSet<int>();
        decimal total = 0;
        foreach (var cartItem in cart.Items)
        {
            var product = cartItem.Product ?? throw new KeyNotFoundException("A cart product was not found.");
            if (cartItem.Quantity > product.Stock)
                throw new BusinessRuleException($"Insufficient stock for {product.Name}.");

            var unitPrice = ProductService.EffectivePrice(product, productDiscounts[product.Id]);
            total += unitPrice * cartItem.Quantity;
            var previousStock = product.Stock;
            product.Stock -= cartItem.Quantity;
            if (product.Store is { ManagerId: not null } store)
            {
                storeIds.Add(store.Id);
                if (previousStock > product.MinimumStockLevel && product.Stock <= product.MinimumStockLevel)
                    lowStockProducts.Add((store.Id, product.Id, product.Name, product.Stock, product.MinimumStockLevel));
            }
            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = cartItem.Quantity,
                Price = unitPrice
            });
        }

        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.Pending,
            TotalAmount = decimal.Round(total, 2),
            PaymentMethod = paymentMethod.ToString(),
            DeliveryMethod = deliveryMethod.ToString(),
            ShippingAddress = new ShippingAddress
            {
                StreetAddress = shippingAddress.StreetAddress.Trim(),
                City = shippingAddress.City.Trim(),
                PostalCode = shippingAddress.PostalCode.Trim(),
                Country = shippingAddress.Country.Trim()
            },
            Items = orderItems
        };
        await orders.AddAsync(order);
        cartItems.DeleteRange(cart.Items);
        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        foreach (var lowStock in lowStockProducts)
            await notifications.NotifyStoreManagerAsync(lowStock.StoreId, NotificationType.LowStock, "Low stock",
                $"{lowStock.ProductName} has stock {lowStock.Stock}, at or below its minimum level of {lowStock.MinimumStockLevel}.",
                lowStock.ProductId);
        foreach (var storeId in storeIds)
            await notifications.NotifyStoreManagerAsync(storeId, NotificationType.NewOrder, "New order received",
                $"Order {order.Id} includes products from your store.", order.Id);

        return ToDto(order);
    }

    private async Task<ShippingAddressRequest> ResolveShippingAddressAsync(int userId, CreateOrderRequest request, DeliveryMethod deliveryMethod)
    {
        if (request.SavedAddressId is int addressId)
        {
            var address = await addresses.Query.FirstOrDefaultAsync(x => x.Id == addressId && x.UserId == userId)
                ?? throw new KeyNotFoundException("Saved address was not found.");
            return new ShippingAddressRequest
            {
                StreetAddress = address.StreetAddress,
                City = address.City,
                PostalCode = address.PostalCode,
                Country = address.Country
            };
        }

        if (request.ShippingAddress is not null) return request.ShippingAddress;
        if (deliveryMethod == DeliveryMethod.Pickup) return new ShippingAddressRequest();
        throw new ArgumentException("Courier delivery requires a saved address or a shipping address.");
    }

    public async Task<OrderDto> UpdateStatusAsync(int id, OrderStatus nextStatus, int? managerId = null)
    {
        if (nextStatus == OrderStatus.Cancelled)
            throw new BusinessRuleException("Use the cancel endpoint to cancel an order.");

        var order = await orders.Query.Include(x => x.User)
            .Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x!.Store)
            .Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x!.Images)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Order was not found.");
        if (managerId is int scopedManagerId && !HasStoreManagerItems(order, scopedManagerId))
            throw new ForbiddenException("You can manage only orders containing products from your stores.");

        ApplyStatusChange(order, nextStatus);
        await orders.SaveAsync();
        await NotifyCustomerOrderStatusChangedAsync(order);
        return ToDto(order, managerId);
    }

    public async Task<OrderDto> CancelAsync(int id, int userId, bool isAdmin, int? managerId = null)
    {
        var order = await orders.Query.Include(x => x.User)
            .Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x!.Store)
            .Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x!.Images)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Order was not found.");
        if (managerId is int scopedManagerId && !HasStoreManagerItems(order, scopedManagerId))
            throw new ForbiddenException("You can manage only orders containing products from your stores.");
        if (!isAdmin && managerId is null && order.UserId != userId)
            throw new ForbiddenException("You can cancel only your own order.");

        ApplyStatusChange(order, OrderStatus.Cancelled);
        await orders.SaveAsync();
        if (!isAdmin && managerId is null)
            await NotifyStoreManagersOfCustomerCancellationAsync(order);
        else
            await NotifyCustomerOrderStatusChangedAsync(order);
        return ToDto(order, managerId);
    }

    public async Task DeleteAsync(int id)
    {
        var order = await orders.Query.Include(x => x.Items).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Order was not found.");

        // Checkout reserves stock until an order is cancelled or fulfilled. If an admin
        // removes an unfulfilled order, return its reserved stock before deleting it.
        if (order.Status is OrderStatus.Pending or OrderStatus.Paid)
        {
            foreach (var item in order.Items)
            {
                if (item.Product is not null)
                    item.Product.Stock += item.Quantity;
            }
        }

        orders.Delete(order);
        await orders.SaveAsync();
    }

    public async Task<List<OrderDto>> GetForUserAsync(int targetUserId, int callerId, bool isAdmin)
    {
        if (!isAdmin && targetUserId != callerId)
            throw new ForbiddenException("You can view only your own orders.");
        var userOrders = await orders.Query.AsNoTracking().Include(x => x.User)
            .Include(x => x.Items).ThenInclude(x => x.Product).ThenInclude(x => x!.Images)
            .Where(x => x.UserId == targetUserId)
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .ToListAsync();
        return userOrders.Select(order => ToDto(order)).ToList();
    }

    private static bool HasStoreManagerItems(Order order, int managerId) =>
        order.Items.Any(item => item.Product?.Store?.ManagerId == managerId);

    private Task NotifyCustomerOrderStatusChangedAsync(Order order) =>
        notifications.NotifyUserAsync(
            order.UserId,
            NotificationType.OrderStatusChanged,
            $"Order #{order.Id} status updated",
            $"Your order #{order.Id} status changed to {order.Status}.",
            order.Id);

    private async Task NotifyStoreManagersOfCustomerCancellationAsync(Order order)
    {
        var managerIds = order.Items
            .Select(item => item.Product?.Store?.ManagerId)
            .Where(managerId => managerId.HasValue)
            .Select(managerId => managerId.GetValueOrDefault())
            .Distinct();

        foreach (var managerId in managerIds)
        {
            await notifications.NotifyUserAsync(
                managerId,
                NotificationType.CustomerOrderCancelled,
                "Customer cancelled an order",
                $"Customer cancelled order #{order.Id}. Its status is now {order.Status}.",
                order.Id);
        }
    }

    private static OrderDto ToDto(Order order, int? managerId = null)
    {
        var items = managerId is int scopedManagerId
            ? order.Items.Where(item => item.Product?.Store?.ManagerId == scopedManagerId).OrderBy(item => item.Id).ToList()
            : order.Items.OrderBy(item => item.Id).ToList();

        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            CustomerName = order.User?.DisplayName ?? string.Empty,
            CustomerEmail = order.User?.Email ?? string.Empty,
            Status = order.Status,
            TotalAmount = managerId is null
                ? order.TotalAmount
                : decimal.Round(items.Sum(item => item.Price * item.Quantity), 2),
            ShippingAddress = new ShippingAddressRequest
            {
                StreetAddress = order.ShippingAddress.StreetAddress,
                City = order.ShippingAddress.City,
                PostalCode = order.ShippingAddress.PostalCode,
                Country = order.ShippingAddress.Country
            },
            PaymentMethod = order.PaymentMethod,
            DeliveryMethod = order.DeliveryMethod,
            CreatedAtUtc = order.CreatedAtUtc,
            Items = items.Select(x => new OrderLineDto
            {
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                ImageUrl = x.Product?.Images.OrderBy(image => image.SortOrder)
                    .Select(image => image.SecureUrl).FirstOrDefault(),
                Quantity = x.Quantity,
                UnitPrice = x.Price,
                LineTotal = x.Price * x.Quantity
            }).ToList()
        };
    }

    private static void ApplyStatusChange(Order order, OrderStatus nextStatus)
    {
        OrderWorkflow.EnsureTransition(order.Status, nextStatus);
        if (nextStatus == OrderStatus.Cancelled && order.Status is OrderStatus.Pending or OrderStatus.Paid)
        {
            foreach (var item in order.Items)
            {
                if (item.Product is not null)
                    item.Product.Stock += item.Quantity;
            }
        }

        order.Status = nextStatus;
    }
}
