using System.ComponentModel.DataAnnotations;
using BizSpace.Dtos;
using BizSpace.Models;
using BizSpace.Repositories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BizSpace.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Roles = UserRoles.Admin + "," + UserRoles.Customer + "," + UserRoles.StoreManager)]
public sealed class OrdersController(IOrderService orders) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetAll()
    {
        var results = IsStoreManager()
            ? await orders.GetForStoreManagerAsync(CurrentUserId)
            : await orders.GetAllAsync(CurrentUserId, IsAdmin());
        return Ok(ApiResponse<List<OrderDto>>.Ok(results));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetById(int id)
    {
        var order = IsStoreManager()
            ? await orders.GetForStoreManagerAsync(id, CurrentUserId)
            : await orders.GetByIdAsync(id, CurrentUserId, IsAdmin());
        return Ok(ApiResponse<OrderDto>.Ok(order));
    }

    [Authorize(Roles = UserRoles.Customer)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderDto>>> Create(
        [FromQuery(Name = "paymentMethod"), BindRequired, EnumDataType(typeof(PaymentMethod))] PaymentMethod paymentMethod,
        [FromQuery(Name = "deliveryMethod"), BindRequired, EnumDataType(typeof(DeliveryMethod))] DeliveryMethod deliveryMethod,
        [FromBody] CreateOrderRequest request)
    {
        var order = await orders.CreateFromCartAsync(CurrentUserId, request, paymentMethod, deliveryMethod);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, ApiResponse<OrderDto>.Ok(order, "Order created."));
    }

    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.StoreManager)]
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateStatus(
        int id,
        [FromQuery(Name = "status"), BindRequired, EnumDataType(typeof(OrderStatusUpdate))] OrderStatusUpdate status) =>
        Ok(ApiResponse<OrderDto>.Ok(
            await orders.UpdateStatusAsync(id, (OrderStatus)status, IsStoreManager() ? CurrentUserId : null),
            "Order status updated."));

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> Cancel(int id) =>
        Ok(ApiResponse<OrderDto>.Ok(await orders.CancelAsync(id, CurrentUserId, IsAdmin(),
            IsStoreManager() ? CurrentUserId : null), "Order cancelled."));

    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await orders.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(null, "Order deleted."));
    }
}
