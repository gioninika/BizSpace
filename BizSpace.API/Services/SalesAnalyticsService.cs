using BizSpace.Dtos;
using BizSpace.Exceptions;
using BizSpace.Models;
using BizSpace.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Repositories.Services;

public sealed class SalesAnalyticsService(IGenericRepository<Order> orders) : ISalesAnalyticsService
{
    public async Task<SalesAnalyticsDto> GetSalesAsync(DateTime? fromUtc, DateTime? toUtc, int? managerId = null)
    {
        var end = toUtc ?? DateTime.UtcNow;
        var start = fromUtc ?? end.AddDays(-30);
        if (start > end)
            throw new ArgumentException("fromUtc must be earlier than or equal to toUtc.");

        var orderList = await orders.Query.AsNoTracking().Include(x => x.Items)
            .ThenInclude(x => x.Product).ThenInclude(x => x!.Store)
            .Where(x => x.CreatedAtUtc >= start && x.CreatedAtUtc <= end)
            .ToListAsync();
        var relevantOrders = orderList.Select(order => new
            {
                Order = order,
                Items = managerId.HasValue
                    ? order.Items.Where(item => item.Product?.Store?.ManagerId == managerId.Value).ToList()
                    : order.Items.ToList()
            })
            .Where(x => !managerId.HasValue || x.Items.Count > 0)
            .ToList();
        var paid = relevantOrders.Where(x => x.Order.Status is OrderStatus.Paid or OrderStatus.Shipped or OrderStatus.Delivered).ToList();
        var soldLines = paid.SelectMany(x => x.Items).ToList();
        var revenue = paid.Sum(x => managerId.HasValue
            ? x.Items.Sum(item => item.Price * item.Quantity)
            : x.Order.TotalAmount);

        return new SalesAnalyticsDto
        {
            FromUtc = start,
            ToUtc = end,
            TotalOrders = relevantOrders.Count,
            PaidOrders = paid.Count,
            CancelledOrders = relevantOrders.Count(x => x.Order.Status == OrderStatus.Cancelled),
            Revenue = decimal.Round(revenue, 2),
            AverageOrderValue = paid.Count == 0 ? 0 : decimal.Round(revenue / paid.Count, 2),
            UnitsSold = soldLines.Sum(x => x.Quantity),
            TopProducts = soldLines
                .GroupBy(x => x.ProductName)
                .Select(group => new TopSellingProductDto
                {
                    ProductName = group.Key,
                    QuantitySold = group.Sum(x => x.Quantity),
                    Revenue = decimal.Round(group.Sum(x => x.Price * x.Quantity), 2)
                })
                .OrderByDescending(x => x.QuantitySold)
                .ThenByDescending(x => x.Revenue)
                .Take(10)
                .ToList()
        };
    }
}
