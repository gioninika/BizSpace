using BizSpace.Models;
using BizSpace.Repositories.Services;
using Xunit;

namespace BizSpace.Tests;

public sealed class OrderWorkflowTests
{
    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Paid)]
    [InlineData(OrderStatus.Paid, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered)]
    public void CanTransition_AllowsNextOrderState(OrderStatus current, OrderStatus next) =>
        Assert.True(OrderWorkflow.CanTransition(current, next));

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Paid, OrderStatus.Pending)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Pending)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Delivered)]
    public void CanTransition_RejectsInvalidOrderState(OrderStatus current, OrderStatus next) =>
        Assert.False(OrderWorkflow.CanTransition(current, next));

    [Fact]
    public void EffectivePrice_AppliesTheProductDiscount()
    {
        var product = new Product { Price = 100m, DiscountPercent = 15m };
        Assert.Equal(85m, product.EffectivePrice);
    }
}
