using BizSpace.Dtos;
using BizSpace.Models;
using BizSpace.Repositories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizSpace.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize(Roles = UserRoles.Customer)]
public sealed class CartController(ICartService carts) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetMine() =>
        Ok(ApiResponse<CartDto>.Ok(await carts.GetMineAsync(CurrentUserId)));

    [HttpPost("items")]
    public async Task<ActionResult<ApiResponse<CartDto>>> AddItem(AddCartItemRequest request) =>
        Ok(ApiResponse<CartDto>.Ok(await carts.AddItemAsync(CurrentUserId, request), "Item added to cart."));

    [HttpPut("items/{id:int}/quantity")]
    public async Task<ActionResult<ApiResponse<CartDto>>> UpdateQuantity(int id, UpdateCartQuantityRequest request) =>
        Ok(ApiResponse<CartDto>.Ok(await carts.UpdateQuantityAsync(CurrentUserId, id, request.Quantity), "Cart quantity updated."));

    [HttpDelete("items/{id:int}")]
    public async Task<ActionResult<ApiResponse<CartDto>>> RemoveItem(int id) =>
        Ok(ApiResponse<CartDto>.Ok(await carts.RemoveItemAsync(CurrentUserId, id), "Item removed from cart."));
}
