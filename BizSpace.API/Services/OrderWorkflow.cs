using BizSpace.Exceptions;
using BizSpace.Models;

namespace BizSpace.Repositories.Services;

public static class OrderWorkflow
{
    public static bool CanTransition(OrderStatus current, OrderStatus next) => (current, next) switch
    {
        (OrderStatus.Pending, OrderStatus.Paid) => true,
        (OrderStatus.Pending, OrderStatus.Cancelled) => true,
        (OrderStatus.Paid, OrderStatus.Shipped) => true,
        (OrderStatus.Paid, OrderStatus.Cancelled) => true,
        (OrderStatus.Shipped, OrderStatus.Delivered) => true,
        (OrderStatus.Shipped, OrderStatus.Cancelled) => true,
        _ => false
    };

    public static void EnsureTransition(OrderStatus current, OrderStatus next)
    {
        if (!CanTransition(current, next))
            throw new BusinessRuleException($"Order status cannot change from {current} to {next}.");
    }
}
